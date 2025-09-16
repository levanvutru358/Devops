using System.Data;
using EmoApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EmoApi.Controllers;

[ApiController]
[Route("api")] // /api/categories, /api/products
public class CatalogController : ControllerBase
{
    private readonly string? _conn;
    public CatalogController(IConfiguration config)
    {
        _conn = config.GetConnectionString("Default");
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, name FROM categories ORDER BY name", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Category>();
        while (await reader.ReadAsync()) list.Add(new Category(reader.GetInt64(0), reader.GetString(1)));
        return Ok(list);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] long? categoryId = null, [FromQuery] string? q = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        var where = new List<string>();
        var parms = new List<MySqlParameter>();
        if (categoryId.HasValue)
        {
            where.Add("category_id=@cat");
            parms.Add(new MySqlParameter("@cat", categoryId.Value));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            where.Add("name LIKE @q");
            parms.Add(new MySqlParameter("@q", "%" + q + "%"));
        }
        var whereSql = where.Count > 0 ? ("WHERE " + string.Join(" AND ", where)) : "";

        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand($"SELECT id, name, description, price, category_id, stock, image_url FROM products {whereSql} ORDER BY id DESC LIMIT @limit OFFSET @offset", conn);
        foreach (var p in parms) cmd.Parameters.Add(p);
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", offset);
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Product>();
        while (await reader.ReadAsync())
        {
            list.Add(new Product(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetDecimal(3), reader.GetInt64(4), reader.GetInt32(5), reader.IsDBNull(6) ? null : reader.GetString(6)));
        }
        return Ok(list);
    }

    [HttpGet("products/{id:long}")]
    public async Task<IActionResult> GetProduct(long id)
    {
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, name, description, price, category_id, stock, image_url FROM products WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync()) return NotFound();
        var p = new Product(reader.GetInt64(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetDecimal(3), reader.GetInt64(4), reader.GetInt32(5), reader.IsDBNull(6) ? null : reader.GetString(6));
        return Ok(p);
    }
}

