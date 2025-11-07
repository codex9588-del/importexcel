using ExcelImporter.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add dependency injection
builder.Services.AddScoped<IDynamicImportService, DynamicImportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable static files and default files - order matters!
app.UseDefaultFiles();  // This must come before UseStaticFiles
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

// Database already exists - no need to create

app.Run();