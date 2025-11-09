
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Keep this if still needed for AddJwtBearer extension

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();

// Configure SQL Server with Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map authentication endpoints
app.MapPost("/api/auth/register", async (RegisterRequest model, ApplicationDbContext dbContext) =>
{
    // Check for existing user in the database
    if (await dbContext.Users.AnyAsync(u => u.Email == model.Email))
    {
        return Results.Conflict("User with this email already exists.");
    }

    var userId = Guid.NewGuid().ToString();
    var newUser = new User { Id = userId, Email = model.Email, UserName = model.UserName, PasswordHash = model.Password };

    dbContext.Users.Add(newUser);
    await dbContext.SaveChangesAsync();

    var token = GenerateMockJwtToken(userId, model.UserName, model.Email);

    return Results.Ok(new AuthResponse(userId, model.UserName, token));
});

app.MapPost("/api/auth/login", async (LoginRequest model, ApplicationDbContext dbContext) =>
{
    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == model.Password);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var token = GenerateMockJwtToken(user.Id, user.UserName, user.Email);

    return Results.Ok(new AuthResponse(user.Id, user.UserName, token));
});

// Helper for generating a mock JWT token (replace with real JWT implementation later)
string GenerateMockJwtToken(string userId, string userName, string email)
{
    return $"mock-jwt-token-for-{userId}-{userName}-{email}";
}

// Map Business Setup endpoint
app.MapPost("/api/business", async (
    BusinessRequest model,
    ApplicationDbContext dbContext,
    IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
{
    // TODO: In a real app, get UserId from authenticated user's JWT token
    var currentUserId = (await dbContext.Users.FirstOrDefaultAsync())?.Id;
    if (string.IsNullOrEmpty(currentUserId))
    {
        return Results.Conflict("No users registered. Please register a user first.");
    }

    // 1. Create Business in SQL Server
    var newBusiness = new Business
    {
        UserId = currentUserId,
        Name = model.Name,
        StartDate = model.StartDate,
        CreatedAt = DateTime.UtcNow
    };

    dbContext.Businesses.Add(newBusiness);
    await dbContext.SaveChangesAsync();

    // 2. Initialize 4 fiscal quarters in MongoDB
    var quarters = GenerateFiscalQuarters(newBusiness.StartDate, newBusiness.Id);
    await quarterlyUpdatesCollection.InsertManyAsync(quarters);

    return Results.Created($"/api/business/{newBusiness.Id}", new BusinessResponse(newBusiness.Id, newBusiness.Name));
});

// Helper for generating fiscal quarters (e.g., for MTD ITSA, starting April 6th)
List<QuarterlyUpdate> GenerateFiscalQuarters(DateTime startDate, int businessId)
{
    var quarters = new List<QuarterlyUpdate>();
    var currentYear = startDate.Year;

    DateTime fiscalYearStart;
    if (startDate.Month < 4 || (startDate.Month == 4 && startDate.Day < 6))
    {
        fiscalYearStart = new DateTime(currentYear - 1, 4, 6);
    }
    else
    {
        fiscalYearStart = new DateTime(currentYear, 4, 6);
    }

    for (int i = 0; i < 4; i++)
    {
        var quarterStartDate = fiscalYearStart.AddMonths(i * 3);
        // var quarterEndDate = quarterStartDate.AddMonths(3).AddDays(-1); // Not used explicitly for this model

        var quarterName = $"Q{(i % 4) + 1}";
        var taxYear = $"{fiscalYearStart.Year}/{fiscalYearStart.Year + 1}";

        quarters.Add(new QuarterlyUpdate
        {
            BusinessId = businessId,
            TaxYear = taxYear,
            QuarterName = quarterName,
            Status = "DRAFT",
            TaxableIncome = 0.00m,
            AllowableExpenses = 0.00m,
            NetProfit = 0.00m
        });
    }
    return quarters;
}

app.Run();
