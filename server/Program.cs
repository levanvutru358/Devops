using MySqlConnector;
using System.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS for local frontend dev (Vite/CRA)
var allowedOrigins = new[]
{
    "http://localhost:5173",
    "https://localhost:5173",
    "http://localhost:3000",
    "https://localhost:3000",
    "http://47.128.79.251:5173",
    "http://18.143.155.245:5173"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection.GetValue<string>("Issuer") ?? "EmoApi";
var jwtAudience = jwtSection.GetValue<string>("Audience") ?? "EmoClient";
var jwtSecret = jwtSection.GetValue<string>("Secret") ?? "dev_secret_change_me_please_dev_secret_change";
var jwtExpires = jwtSection.GetValue<int?>("ExpiresMinutes") ?? 120;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => { options.Title = "Emo API"; });
}

// In containers we usually terminate TLS at proxy; disable redirect by default
var enableHttpsRedirect = builder.Configuration.GetValue<bool>("EnableHttpsRedirect", false);
if (enableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

// Ensure proper order for CORS with endpoint routing
app.UseRouting();

// Enable CORS
app.UseCors("FrontendDev");

// AuthN/Z
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// MySQL connection string & ensure database (wait then create schema)
var connString = builder.Configuration.GetConnectionString("Default");
try
{
    await WaitForDatabaseAsync(connString, TimeSpan.FromSeconds(120));
    await EnsureDatabaseAsync(connString);
    Console.WriteLine("[Startup] Database ready.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Startup] Database init failed: {ex.Message}");
}

app.Run();

static async Task EnsureDatabaseAsync(string? connString)
{
    if (string.IsNullOrWhiteSpace(connString)) return;
    await using var initConn = new MySqlConnection(connString);
    await initConn.OpenAsync();
    const string createSql = @"CREATE TABLE IF NOT EXISTS todos (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        title VARCHAR(255) NOT NULL,
        is_done TINYINT(1) NOT NULL DEFAULT 0,
        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS users (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        email VARCHAR(255) NOT NULL UNIQUE,
        password_hash VARCHAR(255) NOT NULL,
        name VARCHAR(255) NOT NULL,
        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS categories (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        name VARCHAR(255) NOT NULL UNIQUE
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS products (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        name VARCHAR(255) NOT NULL,
        description TEXT NULL,
        price DECIMAL(10,2) NOT NULL,
        category_id BIGINT NOT NULL,
        stock INT NOT NULL DEFAULT 0,
        image_url VARCHAR(1024) NULL,
        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (category_id) REFERENCES categories(id)
            ON UPDATE CASCADE ON DELETE RESTRICT
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS carts (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        user_id BIGINT NOT NULL,
        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        UNIQUE KEY uq_cart_user(user_id),
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON UPDATE CASCADE ON DELETE CASCADE
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS cart_items (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        cart_id BIGINT NOT NULL,
        product_id BIGINT NOT NULL,
        quantity INT NOT NULL,
        UNIQUE KEY uq_cart_product(cart_id, product_id),
        FOREIGN KEY (cart_id) REFERENCES carts(id)
            ON UPDATE CASCADE ON DELETE CASCADE,
        FOREIGN KEY (product_id) REFERENCES products(id)
            ON UPDATE CASCADE ON DELETE RESTRICT
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS orders (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        user_id BIGINT NOT NULL,
        created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        total DECIMAL(12,2) NOT NULL,
        status VARCHAR(32) NOT NULL DEFAULT 'CREATED',
        shipping_name VARCHAR(255) NOT NULL,
        shipping_phone VARCHAR(50) NOT NULL,
        shipping_address VARCHAR(1024) NOT NULL,
        FOREIGN KEY (user_id) REFERENCES users(id)
            ON UPDATE CASCADE ON DELETE CASCADE
    ) ENGINE=InnoDB;

    CREATE TABLE IF NOT EXISTS order_items (
        id BIGINT PRIMARY KEY AUTO_INCREMENT,
        order_id BIGINT NOT NULL,
        product_id BIGINT NOT NULL,
        name VARCHAR(255) NOT NULL,
        price DECIMAL(10,2) NOT NULL,
        quantity INT NOT NULL,
        FOREIGN KEY (order_id) REFERENCES orders(id)
            ON UPDATE CASCADE ON DELETE CASCADE,
        FOREIGN KEY (product_id) REFERENCES products(id)
            ON UPDATE CASCADE ON DELETE RESTRICT
    ) ENGINE=InnoDB;";
    await using var cmd = new MySqlCommand(createSql, initConn);
    await cmd.ExecuteNonQueryAsync();
}

static async Task WaitForDatabaseAsync(string? connString, TimeSpan timeout)
{
    if (string.IsNullOrWhiteSpace(connString)) return;
    var started = DateTime.UtcNow;
    Exception? last = null;
    while (DateTime.UtcNow - started < timeout)
    {
        try
        {
            await using var c = new MySqlConnection(connString);
            await c.OpenAsync();
            await c.CloseAsync();
            return;
        }
        catch (Exception ex)
        {
            last = ex;
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
    Console.Error.WriteLine($"[Startup] Could not connect to DB within {timeout.TotalSeconds}s: {last?.Message}");
}
