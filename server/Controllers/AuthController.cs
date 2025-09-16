using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmoApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Data;

namespace EmoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly string? _conn;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secret;
    private readonly int _expires;

    public AuthController(IConfiguration config)
    {
        _conn = config.GetConnectionString("Default");
        var jwt = config.GetSection("Jwt");
        _issuer = jwt.GetValue<string>("Issuer") ?? "EmoApi";
        _audience = jwt.GetValue<string>("Audience") ?? "EmoClient";
        _secret = jwt.GetValue<string>("Secret") ?? "dev_secret_change_me_please_dev_secret_change";
        _expires = jwt.GetValue<int?>("ExpiresMinutes") ?? 120;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Email, password, name are required.");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        try
        {
            await using var cmd = new MySqlCommand("INSERT INTO users (email, password_hash, name) VALUES (@e, @p, @n); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@e", req.Email);
            cmd.Parameters.AddWithValue("@p", hash);
            cmd.Parameters.AddWithValue("@n", req.Name);
            var id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            // Ensure cart exists
            await using var cartCmd = new MySqlCommand("INSERT INTO carts (user_id) VALUES (@u) ON DUPLICATE KEY UPDATE user_id = user_id", conn);
            cartCmd.Parameters.AddWithValue("@u", id);
            await cartCmd.ExecuteNonQueryAsync();
            var token = GenerateJwt(id.ToString(), req.Email, req.Name);
            return Ok(new AuthResponse(token));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Conflict("Email already registered.");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(_conn)) return Problem("Connection string not configured.");
        await using var conn = new MySqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, email, password_hash, name FROM users WHERE email=@e", conn);
        cmd.Parameters.AddWithValue("@e", req.Email);
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await reader.ReadAsync()) return Unauthorized();
        var id = reader.GetInt64(0);
        var email = reader.GetString(1);
        var hash = reader.GetString(2);
        var name = reader.GetString(3);
        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash)) return Unauthorized();
        var token = GenerateJwt(id.ToString(), email, name);
        return Ok(new AuthResponse(token));
    }

    private string GenerateJwt(string userId, string email, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new("name", name),
            new(ClaimTypes.NameIdentifier, userId)
        };
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_expires),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
