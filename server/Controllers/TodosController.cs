using System.Data;
using EmoApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EmoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly string? _conn;
    public TodosController(IConfiguration config)
    {
        _conn = config.GetConnectionString("Default");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, title, is_done, created_at FROM todos ORDER BY id DESC", conn);
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        var list = new List<Todo>();
        while (await reader.ReadAsync())
        {
            list.Add(new Todo(reader.GetInt64(0), reader.GetString(1), Convert.ToBoolean(reader.GetValue(2)), reader.GetDateTime(3)));
        }
        return Ok(list);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, title, is_done, created_at FROM todos WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (await reader.ReadAsync())
        {
            var todo = new Todo(reader.GetInt64(0), reader.GetString(1), Convert.ToBoolean(reader.GetValue(2)), reader.GetDateTime(3));
            return Ok(todo);
        }
        return NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TodoCreate input)
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        const string sql = "INSERT INTO todos (title, is_done) VALUES (@title, @is_done); SELECT LAST_INSERT_ID();";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@title", input.Title);
        cmd.Parameters.AddWithValue("@is_done", input.IsDone);
        var idObj = await cmd.ExecuteScalarAsync();
        var id = Convert.ToInt64(idObj);

        await using var getCmd = new MySqlCommand("SELECT id, title, is_done, created_at FROM todos WHERE id=@id", conn);
        getCmd.Parameters.AddWithValue("@id", id);
        await using var reader = await getCmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        await reader.ReadAsync();
        var created = new Todo(reader.GetInt64(0), reader.GetString(1), Convert.ToBoolean(reader.GetValue(2)), reader.GetDateTime(3));
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TodoCreate input)
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        const string sql = "UPDATE todos SET title=@title, is_done=@is_done WHERE id=@id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@title", input.Title);
        cmd.Parameters.AddWithValue("@is_done", input.IsDone);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("DELETE FROM todos WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0) return NotFound();
        return NoContent();
    }
}

