using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using api.Endpoints;
using api.Filters;
using Microsoft.OpenApi.Models; // NEW: Required for OpenApiInfo, OpenApiSecurityScheme etc.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// REMOVED: builder.Services.AddOpenApi();
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();

// NEW: Required for Swashbuckle.AspNetCore to discover endpoints
builder.Services.AddEndpointsApiExplorer();
// NEW: Add Swagger generation services (from Swashbuckle)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MTD-ITSA API", Version = "v1" });

    // Configure Swagger to include "Bearer" authorization in the generated spec
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure SQL Server with Entity Framework Core
if (!builder.Environment.IsEnvironment("IntegrationTests"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(settings.DatabaseName);
    return database.GetCollection<QuarterlyUpdate>(settings.QuarterlyUpdatesCollectionName);
});

// Register the custom endpoint filter
builder.Services.AddScoped<AuthAndBusinessFilter>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // Transient Fault Handling: This allows the application to retry SQL commands
            // if the server is temporarily unavailable (e.g., still initializing).
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        }
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
// REMOVED: app.MapOpenApi();
// NEW: Enable Swagger JSON endpoint. This does NOT serve the UI.
app.UseSwagger();

if (app.Environment.IsDevelopment())
{
    // REMOVED: app.UseSwaggerUI(...) as we don't want the UI
}

app.UseHttpsRedirection();

// Map Endpoints using extension methods
app.MapAuthEndpoints();
app.MapBusinessEndpoints();
app.MapQuarterlyUpdateEndpoints();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // CRITICAL: This line creates the database if it doesn't exist and applies all pending migrations.
        await context.Database.MigrateAsync();
        Console.WriteLine("Database migration complete.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();

public partial class Program { } // Make the Program class accessible to integration tests
