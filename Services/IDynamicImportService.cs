using ExcelImporter.DTOs;

namespace ExcelImporter.Services;

public interface IDynamicImportService
{
    Task<ImportResultDto> ImportExcelToDynamicTableAsync(string databaseName, string tableName, IFormFile file);
    Task<object> GetTableDataAsync(string databaseName, string tableName);
}