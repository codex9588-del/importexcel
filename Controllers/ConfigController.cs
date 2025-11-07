using Microsoft.AspNetCore.Mvc;

namespace ExcelImporter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("connection")]
    public IActionResult UpdateConnection([FromBody] ConnectionRequest request)
    {
        try
        {
            var connectionString = $"Host={request.Host};Database={request.Database};Username={request.Username};Password={request.Password};Port={request.Port}";
            
            // Update configuration in memory
            _configuration["ConnectionStrings:DefaultConnection"] = connectionString;
            
            return Ok(new { message = "Connection updated successfully", connectionString });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("connection")]
    public IActionResult GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var parts = connectionString?.Split(';').ToDictionary(
            part => part.Split('=')[0], 
            part => part.Split('=')[1]
        ) ?? new Dictionary<string, string>();

        return Ok(new
        {
            host = parts.GetValueOrDefault("Host", "localhost"),
            database = parts.GetValueOrDefault("Database", ""),
            username = parts.GetValueOrDefault("Username", ""),
            port = parts.GetValueOrDefault("Port", "5432")
        });
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] ConnectionRequest request)
    {
        try
        {
            var connectionString = $"Host={request.Host};Database=postgres;Username={request.Username};Password={request.Password};Port={request.Port}";
            
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            return Ok(new { success = true, message = "Connection successful" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("databases")]
    public async Task<IActionResult> GetDatabases([FromQuery] string host, [FromQuery] string username, [FromQuery] string password, [FromQuery] string port = "5432")
    {
        try
        {
            var connectionString = $"Host={host};Database=postgres;Username={username};Password={password};Port={port}";
            
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = "SELECT datname FROM pg_database WHERE datistemplate = false";
            using var command = new Npgsql.NpgsqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            var databases = new List<string>();
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
            
            return Ok(databases);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables([FromQuery] string host, [FromQuery] string database, [FromQuery] string username, [FromQuery] string password, [FromQuery] string port = "5432")
    {
        try
        {
            var connectionString = $"Host={host};Database={database};Username={username};Password={password};Port={port}";
            
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                ORDER BY table_name";
                
            using var command = new Npgsql.NpgsqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            
            return Ok(tables);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class ConnectionRequest
{
    public string Host { get; set; } = "localhost";
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Port { get; set; } = "5432";
}