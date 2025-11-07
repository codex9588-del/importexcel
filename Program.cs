using ExcelImporter.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database connection based on environment
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction())
{
    // Use environment variables in production
    connectionString = string.Format(
        connectionString ?? "Host={0};Database={1};Username={2};Password={3};Port={4}",
        Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost",
        Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "postgres",
        Environment.GetEnvironmentVariable("DATABASE_USER") ?? "postgres",
        Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "",
        Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432"
    );
}
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

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