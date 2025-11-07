using Microsoft.AspNetCore.Mvc;
using ExcelImporter.Services;

namespace ExcelImporter.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DynamicImportController : ControllerBase
{
    private readonly IDynamicImportService _dynamicImportService;

    public DynamicImportController(IDynamicImportService dynamicImportService)
    {
        _dynamicImportService = dynamicImportService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel([FromForm] string databaseName, [FromForm] string tableName, [FromForm] IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return BadRequest("Database name is required.");
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            return BadRequest("Table name is required.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            var result = await _dynamicImportService.ImportExcelToDynamicTableAsync(databaseName, tableName, file);
            
            if (result.Errors.Any() && result.SuccessfulImports == 0)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetTableData([FromQuery] string databaseName, [FromQuery] string tableName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return BadRequest("Database name is required.");
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            return BadRequest("Table name is required.");
        }

        try
        {
            var data = await _dynamicImportService.GetTableDataAsync(databaseName, tableName);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}