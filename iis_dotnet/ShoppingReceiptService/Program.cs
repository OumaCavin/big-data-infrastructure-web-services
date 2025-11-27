using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Shopping Receipt API", 
        Version = "v1",
        Description = "Web Service for managing shopping receipts with XML/JSON support",
        Contact = new OpenApiContact
        {
            Name = "Cavin Otieno",
            Email = "support@example.com"
        }
    });
    
    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shopping Receipt API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    service = "Shopping Receipt Service",
    version = "1.0.0"
})).WithName("HealthCheck");

// Add root endpoint
app.MapGet("/", () => Results.Redirect("/swagger")).WithName("Root");

app.Run();

/// <summary>
/// Shopping Receipt Service - ASP.NET Core Web API
/// Provides RESTful endpoints for managing shopping receipts in XML and JSON formats.
/// 
/// Features:
/// - Get receipt by ID
/// - Validate receipt structure
/// - Support for XML and JSON formats
/// - Comprehensive error handling
/// - Swagger/OpenAPI documentation
/// 
/// Author: Cavin Otieno
/// Course: SDS 6104 - Big Data Infrastructure, Platforms and Warehousing
/// Date: 2025-11-27
/// </summary>