using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EmoApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;

namespace EmoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly string? _conn;
    public CartController(IConfiguration config) { _conn = config.GetConnectionString("Default"); }

    private long GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return long.Parse(sub!);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using (var ensure = new MySqlCommand("INSERT INTO carts (user_id) VALUES (@u) ON DUPLICATE KEY UPDATE user_id=user_id", conn))
        { ensure.Parameters.AddWithValue("@u", userId); await ensure.ExecuteNonQueryAsync(); }

        await using var cmd = new MySqlCommand(@"SELECT ci.id, ci.product_id, p.name, p.price, ci.quantity
                                             FROM cart_items ci
                                             JOIN carts c ON c.id = ci.cart_id
                                             JOIN products p ON p.id = ci.product_id
                                             WHERE c.user_id=@u
                                             ORDER BY ci.id DESC", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        await using var reader = await cmd.ExecuteReaderAsync();
        var items = new List<CartItemDto>();
        while (await reader.ReadAsync()) items.Add(new CartItemDto(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4)));
        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] CreateCartItemRequest req)
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using (var ensure = new MySqlCommand("INSERT INTO carts (user_id) VALUES (@u) ON DUPLICATE KEY UPDATE user_id=user_id", conn))
        { ensure.Parameters.AddWithValue("@u", userId); await ensure.ExecuteNonQueryAsync(); }
        long cartId;
        await using (var getCart = new MySqlCommand("SELECT id FROM carts WHERE user_id=@u", conn))
        { getCart.Parameters.AddWithValue("@u", userId); cartId = Convert.ToInt64(await getCart.ExecuteScalarAsync()); }

        await using var stockCmd = new MySqlCommand("SELECT stock FROM products WHERE id=@p", conn);
        stockCmd.Parameters.AddWithValue("@p", req.ProductId);
        var stockObj = await stockCmd.ExecuteScalarAsync();
        if (stockObj == null) return BadRequest("Product not found");
        var stock = Convert.ToInt32(stockObj);
        if (req.Quantity < 1) return BadRequest("Quantity must be >= 1");
        if (req.Quantity > stock) return BadRequest("Not enough stock");

        await using var upsert = new MySqlCommand(@"INSERT INTO cart_items (cart_id, product_id, quantity)
                                               VALUES (@c, @p, @q)
                                               ON DUPLICATE KEY UPDATE quantity = LEAST(quantity + VALUES(quantity), @stock)", conn);
        upsert.Parameters.AddWithValue("@c", cartId);
        upsert.Parameters.AddWithValue("@p", req.ProductId);
        upsert.Parameters.AddWithValue("@q", req.Quantity);
        upsert.Parameters.AddWithValue("@stock", stock);
        await upsert.ExecuteNonQueryAsync();
        return NoContent();
    }

    [HttpPut("items/{itemId:long}")]
    public async Task<IActionResult> UpdateItem(long itemId, [FromBody] UpdateCartItemRequest req)
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        if (req.Quantity < 1) return BadRequest("Quantity must be >= 1");
        await using var info = new MySqlCommand(@"SELECT ci.product_id, p.stock
                                            FROM cart_items ci
                                            JOIN carts c ON c.id = ci.cart_id
                                            JOIN products p ON p.id = ci.product_id
                                            WHERE ci.id=@id AND c.user_id=@u", conn);
        info.Parameters.AddWithValue("@id", itemId);
        info.Parameters.AddWithValue("@u", userId);
        await using var r = await info.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await r.ReadAsync()) return NotFound();
        var stock = r.GetInt32(1);
        await r.DisposeAsync();
        if (req.Quantity > stock) return BadRequest("Not enough stock");
        await using var upd = new MySqlCommand("UPDATE cart_items SET quantity=@q WHERE id=@id", conn);
        upd.Parameters.AddWithValue("@q", req.Quantity);
        upd.Parameters.AddWithValue("@id", itemId);
        await upd.ExecuteNonQueryAsync();
        return NoContent();
    }

    [HttpDelete("items/{itemId:long}")]
    public async Task<IActionResult> DeleteItem(long itemId)
    {
        var userId = GetUserId();
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var del = new MySqlCommand(@"DELETE ci FROM cart_items ci
                                           JOIN carts c ON c.id = ci.cart_id
                                           WHERE ci.id=@id AND c.user_id=@u", conn);
        del.Parameters.AddWithValue("@id", itemId);
        del.Parameters.AddWithValue("@u", userId);
        var affected = await del.ExecuteNonQueryAsync();
        if (affected == 0) return NotFound();
        return NoContent();
    }
}
