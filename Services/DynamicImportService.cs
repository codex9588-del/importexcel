using OfficeOpenXml;
using ExcelImporter.DTOs;
using Npgsql;
using System.Data;

namespace ExcelImporter.Services;

public class DynamicImportService : IDynamicImportService
{
    private readonly IConfiguration _configuration;

    public DynamicImportService(IConfiguration configuration)
    {
        _configuration = configuration;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<ImportResultDto> ImportExcelToDynamicTableAsync(string databaseName, string tableName, IFormFile file)
    {
        var result = new ImportResultDto();

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                result.Errors.Add("❌ Database name is required");
                return result;
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                result.Errors.Add("❌ Table name is required");
                return result;
            }

            if (!IsValidExcelFile(file))
            {
                result.Errors.Add("❌ Invalid file format. Only .xlsx and .xls files are allowed");
                return result;
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                result.Errors.Add("❌ No worksheet found in the Excel file");
                return result;
            }

            var headers = GetHeaders(worksheet);
            if (!headers.Any())
            {
                result.Errors.Add("❌ No headers found in row 1 of Excel file");
                return result;
            }

            result.DetectedColumns = headers;
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            
            if (rowCount < 2)
            {
                result.Errors.Add("❌ Excel file must have at least 2 rows (headers + data)");
                return result;
            }

            result.TotalRecords = Math.Max(0, rowCount - 1);

            var connectionString = GetConnectionString(databaseName);
            
            using var connection = new NpgsqlConnection(connectionString);
            
            try
            {
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"❌ Database connection failed: {ex.Message}");
                return result;
            }

            // Verify database exists
            if (!await DatabaseExistsAsync(connection, databaseName))
            {
                result.Errors.Add($"❌ Database '{databaseName}' does not exist");
                return result;
            }

            // Get table columns
            var tableColumns = await GetTableColumnsAsync(connection, tableName);
            if (!tableColumns.Any())
            {
                result.Errors.Add($"❌ Table '{tableName}' not found in database '{databaseName}'");
                return result;
            }

            // Match Excel headers with table columns
            var columnMapping = MapExcelToTableColumns(headers, tableColumns);
            
            // Check for unmapped columns
            var unmappedColumns = headers.Where(h => !columnMapping.ContainsKey(h)).ToList();
            if (unmappedColumns.Any())
            {
                result.Errors.Add($"⚠️ Excel columns not found in table: {string.Join(", ", unmappedColumns)}");
            }

            if (!columnMapping.Any())
            {
                result.Errors.Add($"❌ No matching columns found between Excel and table '{tableName}'");
                result.Errors.Add($"📋 Excel columns: {string.Join(", ", headers)}");
                result.Errors.Add($"📋 Table columns: {string.Join(", ", tableColumns)}");
                return result;
            }

            result.Errors.Add($"✅ Matched columns: {string.Join(", ", columnMapping.Keys)}");

            // Process rows
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var validationError = ValidateRowData(worksheet, row, columnMapping);
                    if (!string.IsNullOrEmpty(validationError))
                    {
                        result.Errors.Add($"Row {row}: {validationError}");
                        result.FailedImports++;
                        continue;
                    }

                    await InsertRowAsync(connection, tableName, worksheet, row, columnMapping);
                    result.SuccessfulImports++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"❌ Row {row}: {GetFriendlyErrorMessage(ex)}");
                    result.FailedImports++;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"❌ System error: {ex.Message}");
            return result;
        }
    }

    public async Task<object> GetTableDataAsync(string databaseName, string tableName)
    {
        var connectionString = GetConnectionString(databaseName);
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var query = $"SELECT * FROM {tableName} ORDER BY 1 DESC LIMIT 100";
        
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var results = new List<Dictionary<string, object>>();
        
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }
        
        return results;
    }

    private string GetConnectionString(string databaseName)
    {
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = databaseName
        };
        return builder.ToString();
    }

    private bool IsValidExcelFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension) && file.Length > 0;
    }

    private List<string> GetHeaders(ExcelWorksheet worksheet)
    {
        var headers = new List<string>();
        var colCount = worksheet.Dimension?.Columns ?? 0;
        
        for (int col = 1; col <= colCount; col++)
        {
            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                headers.Add(headerValue);
            }
        }
        
        return headers;
    }

    private async Task<List<string>> GetTableColumnsAsync(NpgsqlConnection connection, string tableName)
    {
        var query = @"
            SELECT column_name 
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            ORDER BY ordinal_position";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        
        var columns = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
        
        return columns;
    }

    private async Task<bool> DatabaseExistsAsync(NpgsqlConnection connection, string databaseName)
    {
        try
        {
            var query = "SELECT 1 FROM pg_database WHERE datname = @dbName";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@dbName", databaseName);
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    private Dictionary<string, string> MapExcelToTableColumns(List<string> excelHeaders, List<string> tableColumns)
    {
        var mapping = new Dictionary<string, string>();
        
        foreach (var excelHeader in excelHeaders)
        {
            var matchingColumn = tableColumns.FirstOrDefault(tc => 
                tc.Equals(excelHeader, StringComparison.OrdinalIgnoreCase));
            
            if (matchingColumn != null)
            {
                mapping[excelHeader] = matchingColumn;
            }
        }
        
        return mapping;
    }

    private string ValidateRowData(ExcelWorksheet worksheet, int row, Dictionary<string, string> columnMapping)
    {
        var errors = new List<string>();
        
        foreach (var mapping in columnMapping)
        {
            var excelColIndex = GetExcelColumnIndex(worksheet, mapping.Key);
            var cellValue = worksheet.Cells[row, excelColIndex].Value;
            
            // Check for required fields (basic validation)
            if (cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                // Skip validation for now - let database handle constraints
                continue;
            }
        }
        
        return errors.Any() ? string.Join(", ", errors) : string.Empty;
    }

    private string GetFriendlyErrorMessage(Exception ex)
    {
        if (ex.Message.Contains("duplicate key"))
            return "Duplicate record - this data already exists";
        
        if (ex.Message.Contains("foreign key"))
            return "Invalid reference - related record not found";
        
        if (ex.Message.Contains("not null"))
            return "Required field is empty";
        
        if (ex.Message.Contains("invalid input syntax"))
            return "Invalid data format";
        
        if (ex.Message.Contains("numeric"))
            return "Invalid number format";
        
        if (ex.Message.Contains("timestamp"))
            return "Invalid date/time format";
        
        return ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
    }

    private async Task InsertRowAsync(NpgsqlConnection connection, string tableName, ExcelWorksheet worksheet, int row, Dictionary<string, string> columnMapping)
    {
        if (!columnMapping.Any()) return;

        // Get column types
        var columnTypes = await GetColumnTypesAsync(connection, tableName);
        
        var columns = string.Join(", ", columnMapping.Values);
        var parameters = string.Join(", ", columnMapping.Values.Select((_, i) => $"@param{i}"));
        
        var query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
        
        using var command = new NpgsqlCommand(query, connection);
        
        int paramIndex = 0;
        foreach (var mapping in columnMapping)
        {
            var excelColIndex = GetExcelColumnIndex(worksheet, mapping.Key);
            var cellValue = worksheet.Cells[row, excelColIndex].Value;
            var columnName = mapping.Value;
            
            // Convert value based on column type
            var convertedValue = ConvertValueByType(cellValue, columnTypes.GetValueOrDefault(columnName, "text"));
            
            command.Parameters.AddWithValue($"@param{paramIndex}", convertedValue ?? DBNull.Value);
            paramIndex++;
        }
        
        await command.ExecuteNonQueryAsync();
    }

    private async Task<Dictionary<string, string>> GetColumnTypesAsync(NpgsqlConnection connection, string tableName)
    {
        var query = @"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = @tableName";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        
        var columnTypes = new Dictionary<string, string>();
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            columnTypes[reader.GetString(0)] = reader.GetString(1);
        }
        
        return columnTypes;
    }

    private object? ConvertValueByType(object? value, string dataType)
    {
        if (value == null) return DBNull.Value;
        
        var stringValue = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(stringValue)) return DBNull.Value;
        
        return dataType.ToLower() switch
        {
            "integer" or "int4" or "bigint" or "int8" => 
                int.TryParse(stringValue, out var intVal) ? intVal : DBNull.Value,
            
            "numeric" or "decimal" or "money" => 
                decimal.TryParse(stringValue, out var decVal) ? decVal : DBNull.Value,
            
            "timestamp" or "timestamptz" or "timestamp without time zone" or "timestamp with time zone" => 
                DateTime.TryParse(stringValue, out var dateVal) ? dateVal : DBNull.Value,
            
            "date" => 
                DateOnly.TryParse(stringValue, out var dateOnlyVal) ? dateOnlyVal.ToDateTime(TimeOnly.MinValue) : DBNull.Value,
            
            "boolean" or "bool" => 
                bool.TryParse(stringValue, out var boolVal) ? boolVal : DBNull.Value,
            
            "real" or "float4" or "double precision" or "float8" => 
                double.TryParse(stringValue, out var doubleVal) ? doubleVal : DBNull.Value,
            
            _ => stringValue
        };
    }

    private int GetExcelColumnIndex(ExcelWorksheet worksheet, string headerName)
    {
        var colCount = worksheet.Dimension?.Columns ?? 0;
        
        for (int col = 1; col <= colCount; col++)
        {
            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
            if (headerValue?.Equals(headerName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return col;
            }
        }
        
        return 1; // Default to first column
    }
}