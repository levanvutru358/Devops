using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EmoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EmoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly string? _conn;
    public OrdersController(IConfiguration config) { _conn = config.GetConnectionString("Default"); }
    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var items = new List<(long productId, string name, decimal price, int quantity, int stock)>();
            await using (var cmd = new MySqlCommand(@"SELECT p.id, p.name, p.price, ci.quantity, p.stock
                                                     FROM cart_items ci
                                                     JOIN carts c ON c.id = ci.cart_id
                                                     JOIN products p ON p.id = ci.product_id
                                                     WHERE c.user_id=@u", conn, (MySqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    items.Add((reader.GetInt64(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt32(3), reader.GetInt32(4)));
            }
            if (items.Count == 0) { await tx.RollbackAsync(); return BadRequest("Cart is empty"); }

            decimal total = 0m;
            foreach (var it in items)
            {
                if (it.quantity > it.stock) { await tx.RollbackAsync(); return BadRequest($"Not enough stock for {it.name}"); }
                total += it.price * it.quantity;
            }

            long orderId;
            await using (var createOrder = new MySqlCommand(@"INSERT INTO orders (user_id, total, status, shipping_name, shipping_phone, shipping_address)
                                                             VALUES (@u, @t, 'CREATED', @n, @ph, @addr);
                                                             SELECT LAST_INSERT_ID();", conn, (MySqlTransaction)tx))
            {
                createOrder.Parameters.AddWithValue("@u", userId);
                createOrder.Parameters.AddWithValue("@t", total);
                createOrder.Parameters.AddWithValue("@n", req.ShippingName);
                createOrder.Parameters.AddWithValue("@ph", req.ShippingPhone);
                createOrder.Parameters.AddWithValue("@addr", req.ShippingAddress);
                orderId = Convert.ToInt64(await createOrder.ExecuteScalarAsync());
            }

            foreach (var it in items)
            {
                await using var oi = new MySqlCommand(@"INSERT INTO order_items (order_id, product_id, name, price, quantity)
                                                       VALUES (@o, @p, @n, @pr, @q);", conn, (MySqlTransaction)tx);
                oi.Parameters.AddWithValue("@o", orderId);
                oi.Parameters.AddWithValue("@p", it.productId);
                oi.Parameters.AddWithValue("@n", it.name);
                oi.Parameters.AddWithValue("@pr", it.price);
                oi.Parameters.AddWithValue("@q", it.quantity);
                await oi.ExecuteNonQueryAsync();

                await using var reduce = new MySqlCommand("UPDATE products SET stock = stock - @q WHERE id=@p", conn, (MySqlTransaction)tx);
                reduce.Parameters.AddWithValue("@q", it.quantity);
                reduce.Parameters.AddWithValue("@p", it.productId);
                await reduce.ExecuteNonQueryAsync();
            }

            await using (var clear = new MySqlCommand(@"DELETE ci FROM cart_items ci
                                                   JOIN carts c ON c.id = ci.cart_id
                                                   WHERE c.user_id=@u", conn, (MySqlTransaction)tx))
            { clear.Parameters.AddWithValue("@u", userId); await clear.ExecuteNonQueryAsync(); }

            await tx.CommitAsync();
            return Created($"/api/orders/{orderId}", new { id = orderId, total });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, created_at, total, status FROM orders WHERE user_id=@u ORDER BY id DESC", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        await using var r = await cmd.ExecuteReaderAsync();
        var list = new List<OrderDto>();
        while (await r.ReadAsync()) list.Add(new OrderDto(r.GetInt64(0), r.GetDateTime(1), r.GetDecimal(2), r.GetString(3)));
        return Ok(list);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using (var check = new MySqlCommand("SELECT COUNT(*) FROM orders WHERE id=@id AND user_id=@u", conn))
        { check.Parameters.AddWithValue("@id", id); check.Parameters.AddWithValue("@u", userId); var cnt = Convert.ToInt32(await check.ExecuteScalarAsync()); if (cnt == 0) return NotFound(); }

        await using var items = new MySqlCommand("SELECT product_id, name, price, quantity FROM order_items WHERE order_id=@id", conn);
        items.Parameters.AddWithValue("@id", id);
        await using var r = await items.ExecuteReaderAsync();
        var list = new List<OrderItemDto>();
        while (await r.ReadAsync()) list.Add(new OrderItemDto(r.GetInt64(0), r.GetString(1), r.GetDecimal(2), r.GetInt32(3)));
        return Ok(list);
    }
}

