# 📊 Dynamic Excel Importer

A powerful .NET 8 Web API with modern UI for importing Excel files into any PostgreSQL database table dynamically.

## ✨ Features

- 🎯 **Dynamic Import** - Works with any database, any table, any Excel structure
- 🔧 **Frontend Configuration** - Database setup via web UI
- 📁 **Smart File Handling** - Drag & drop Excel files with validation
- 🔄 **Auto Type Conversion** - Automatic data type handling
- 📊 **Real-time Results** - Live import statistics and error reporting
- 🎨 **Modern UI** - Bootstrap-based responsive interface
- ⚡ **No Code Changes** - Add new columns without touching code

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- PostgreSQL database

### Installation
```bash
git clone https://github.com/yourusername/dynamic-excel-importer.git
cd dynamic-excel-importer
dotnet restore
dotnet run
```

### Usage
1. Open `http://localhost:5000`
2. Configure database connection
3. Select database and table
4. Upload Excel file
5. View results!

## 🎯 How It Works

### 1. Database Configuration
- Enter PostgreSQL connection details
- Test connection
- Browse available databases
- Select target table

### 2. Excel Upload
- Drag & drop or browse Excel files
- Automatic column detection
- Smart column mapping to database fields
- Real-time validation

### 3. Import Process
- Automatic data type conversion
- Row-by-row error handling
- Detailed progress reporting
- Success/failure statistics

## 📋 Supported Features

### File Formats
- ✅ Excel (.xlsx)
- ✅ Excel 97-2003 (.xls)

### Data Types
- ✅ Text/String
- ✅ Numbers (Integer, Decimal)
- ✅ Dates/Timestamps
- ✅ Boolean
- ✅ NULL values

### Error Handling
- ✅ Column mismatch detection
- ✅ Data validation errors
- ✅ Duplicate record handling
- ✅ Foreign key constraint errors
- ✅ User-friendly error messages

## 🔧 API Endpoints

### Dynamic Import
```http
POST /api/DynamicImport/upload
Content-Type: multipart/form-data

Parameters:
- databaseName: string
- tableName: string  
- file: Excel file
```

### View Data
```http
GET /api/DynamicImport/data?databaseName={db}&tableName={table}
```

### Configuration
```http
POST /api/Config/connection
GET /api/Config/connection
POST /api/Config/test
GET /api/Config/databases
GET /api/Config/tables
```

## 🏗️ Architecture

```
📁 Controllers/
├── ConfigController.cs          # Database configuration
└── DynamicImportController.cs   # Excel import logic

📁 Services/
├── IDynamicImportService.cs     # Service interface
└── DynamicImportService.cs      # Core import logic

📁 DTOs/
└── ImportResultDto.cs           # Response models

📁 wwwroot/
├── index.html                   # Frontend UI
└── app.js                       # JavaScript logic
```

## 🎨 Screenshots

### Main Interface
- Modern Bootstrap UI
- Drag & drop file upload
- Real-time configuration

### Import Results
- Success/failure statistics
- Detailed error reporting
- Column mapping information

### Data Viewer
- Formatted table display
- Responsive design
- Export capabilities

## 🔒 Security Features

- Input validation
- SQL injection prevention
- File type validation
- Connection string security

## 🚀 Deployment

### Docker (Recommended)
```bash
docker build -t excel-importer .
docker run -p 5000:80 excel-importer
```

### IIS/Azure
```bash
dotnet publish -c Release
# Deploy to your preferred hosting
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [EPPlus](https://github.com/EPPlusSoftware/EPPlus) for Excel processing
- [Bootstrap](https://getbootstrap.com/) for UI components
- [Font Awesome](https://fontawesome.com/) for icons

## 📞 Support
+91 9461459588
yashjanwa88@gmail.com

If you have any questions or issues, please open an issue on GitHub.

---

**Made with ❤️ for the developer community**
