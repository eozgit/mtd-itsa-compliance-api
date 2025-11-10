
using api.Models;
using api.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
    // Format: mock-jwt-token-for-<userId>-<userName>-<email>
    // userId can contain hyphens, userName and email should not.
    return $"mock-jwt-token-for-{userId}-{userName}-{email}";
}


// NEW HELPER: Extract UserId from the mock Authorization header
string? GetUserIdFromMockToken(string? authorizationHeader)
{
    Console.WriteLine($"DEBUG: GetUserIdFromMockToken received: {authorizationHeader}"); // DIAGNOSTIC
    const string prefix = "Bearer mock-jwt-token-for-";

    if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith(prefix))
    {
        Console.WriteLine("DEBUG: GetUserIdFromMockToken - Header is null/empty or doesn't start with expected prefix."); // DIAGNOSTIC
        return null;
    }

    // Extract the payload string after the prefix
    var tokenPayload = authorizationHeader.Substring(prefix.Length); // e.g., "fe381458-2303-4143-b9e6-6c28fad29dde-TestUser-test@example.com"
    Console.WriteLine($"DEBUG: GetUserIdFromMockToken - Extracted token payload: {tokenPayload}"); // DIAGNOSTIC

    // To robustly extract the UserId when it contains hyphens, we parse from the known end parts (UserName and Email).
    // This assumes UserName and Email themselves do not contain hyphens.
    var lastHyphenIndex = tokenPayload.LastIndexOf('-');
    if (lastHyphenIndex == -1)
    {
        Console.WriteLine("DEBUG: GetUserIdFromMockToken - Not enough parts in tokenPayload (missing email)."); // DIAGNOSTIC
        return null;
    }
    var email = tokenPayload.Substring(lastHyphenIndex + 1);

    var secondLastHyphenIndex = tokenPayload.LastIndexOf('-', lastHyphenIndex - 1);
    if (secondLastHyphenIndex == -1)
    {
        Console.WriteLine("DEBUG: GetUserIdFromMockToken - Not enough parts in tokenPayload (missing userName)."); // DIAGNOSTIC
        return null;
    }
    var userName = tokenPayload.Substring(secondLastHyphenIndex + 1, lastHyphenIndex - (secondLastHyphenIndex + 1));

    // The UserId is everything before the "-{userName}-{email}" part
    var userId = tokenPayload.Substring(0, secondLastHyphenIndex);
    Console.WriteLine($"DEBUG: GetUserIdFromMockToken - Parsed UserId: '{userId}', UserName: '{userName}', Email: '{email}'"); // DIAGNOSTIC

    return userId;
}


// Map Business Setup endpoint
app.MapPost("/api/business", async (
    HttpContext httpContext, // Inject HttpContext to get headers
    BusinessRequest model,
    ApplicationDbContext dbContext,
    IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
{
    // Get UserId from authenticated user's JWT token (mock version)
    var currentUserId = GetUserIdFromMockToken(httpContext.Request.Headers.Authorization.FirstOrDefault());
    Console.WriteLine($"DEBUG: /api/business - currentUserId: {currentUserId}"); // DIAGNOSTIC
    if (string.IsNullOrEmpty(currentUserId))
    {
        Console.WriteLine("DEBUG: /api/business - Unauthorized: currentUserId is null or empty."); // DIAGNOSTIC
        return Results.Unauthorized();
    }

    // Verify user actually exists in the DB
    var userExists = await dbContext.Users.AnyAsync(u => u.Id == currentUserId);
    Console.WriteLine($"DEBUG: /api/business - userExists in DB for ID '{currentUserId}': {userExists}"); // DIAGNOSTIC
    if (!userExists)
    {
        Console.WriteLine("DEBUG: /api/business - Unauthorized: Token user not found in DB."); // DIAGNOSTIC
        return Results.Unauthorized(); // Token user not found in DB
    }

    // Check if the user already has a business (optional, but good for MTD spec)
    if (await dbContext.Businesses.AnyAsync(b => b.UserId == currentUserId))
    {
        return Results.Conflict("User already has a registered business.");
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
    Console.WriteLine($"SQL: Created Business with ID: {newBusiness.Id}");

    // 2. Initialize 4 fiscal quarters in MongoDB
    var quarters = GenerateFiscalQuarters(newBusiness.StartDate, newBusiness.Id);
    Console.WriteLine($"MongoDB: Generated {quarters.Count} quarters for Business ID: {newBusiness.Id}");

    if (quarters.Any())
    {
        await quarterlyUpdatesCollection.InsertManyAsync(quarters);
        Console.WriteLine($"MongoDB: Successfully inserted {quarters.Count} quarterly updates.");
    }
    else
    {
        Console.WriteLine($"MongoDB: No quarters were generated to insert for Business ID: {newBusiness.Id}.");
    }

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
    Console.WriteLine($"DEBUG: GenerateFiscalQuarters - StartDate: {startDate:yyyy-MM-dd}, FiscalYearStart: {fiscalYearStart:yyyy-MM-dd}"); // DIAGNOSTIC LOG

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
        Console.WriteLine($"DEBUG: Added quarter {quarterName} for {taxYear} with BusinessId {businessId}"); // DIAGNOSTIC LOG
    }
    return quarters;
}


app.MapPut("/api/quarter/{id}", async (
    string id, // Quarter ID from route
    QuarterlyUpdateRequest model,
    HttpContext httpContext,
    ApplicationDbContext dbContext,
    IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
{
    var currentUserId = GetUserIdFromMockToken(httpContext.Request.Headers.Authorization.FirstOrDefault());
    if (string.IsNullOrEmpty(currentUserId))
    {
        return Results.Unauthorized();
    }

    var business = await dbContext.Businesses
                                  .Where(b => b.UserId == currentUserId)
                                  .FirstOrDefaultAsync();

    if (business == null)
    {
        return Results.NotFound("No business found for the current user.");
    }

    var quarterToUpdate = await quarterlyUpdatesCollection
                                .Find(q => q.Id == id && q.BusinessId == business.Id)
                                .FirstOrDefaultAsync();

    if (quarterToUpdate == null)
    {
        return Results.NotFound($"Quarterly update with ID '{id}' not found for business ID '{business.Id}'.");
    }

    // Ensure quarter is in DRAFT status before allowing updates
    if (quarterToUpdate.Status != "DRAFT")
    {
        return Results.BadRequest("Only quarters in 'DRAFT' status can be updated.");
    }

    quarterToUpdate.TaxableIncome = model.TaxableIncome;
    quarterToUpdate.AllowableExpenses = model.AllowableExpenses;
    quarterToUpdate.NetProfit = model.TaxableIncome - model.AllowableExpenses; // Recalculate NetProfit

    await quarterlyUpdatesCollection.ReplaceOneAsync(q => q.Id == id, quarterToUpdate);

    return Results.Ok(new
    {
        quarterToUpdate.Id,
        quarterToUpdate.BusinessId,
        quarterToUpdate.TaxYear,
        quarterToUpdate.QuarterName,
        quarterToUpdate.TaxableIncome,
        quarterToUpdate.AllowableExpenses,
        quarterToUpdate.NetProfit,
        quarterToUpdate.Status,
        Message = "Draft saved."
    });
});


app.MapPost("/api/quarter/{id}/submit", async (
    string id, // Quarter ID from route
    HttpContext httpContext,
    ApplicationDbContext dbContext,
    IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
{
    var currentUserId = GetUserIdFromMockToken(httpContext.Request.Headers.Authorization.FirstOrDefault());
    if (string.IsNullOrEmpty(currentUserId))
    {
        return Results.Unauthorized();
    }

    var business = await dbContext.Businesses
                                  .Where(b => b.UserId == currentUserId)
                                  .FirstOrDefaultAsync();

    if (business == null)
    {
        return Results.NotFound("No business found for the current user.");
    }

    var quarterToSubmit = await quarterlyUpdatesCollection
                                .Find(q => q.Id == id && q.BusinessId == business.Id)
                                .FirstOrDefaultAsync();

    if (quarterToSubmit == null)
    {
        return Results.NotFound($"Quarterly update with ID '{id}' not found for business ID '{business.Id}'.");
    }

    // Ensure quarter is in DRAFT status before allowing submission
    if (quarterToSubmit.Status != "DRAFT")
    {
        return Results.BadRequest("Only quarters in 'DRAFT' status can be submitted.");
    }

    // Update status and add submission details
    quarterToSubmit.Status = "SUBMITTED";
    quarterToSubmit.SubmissionDetails = new SubmissionDetails
    {
        RefNumber = $"MTD-ACK-{Guid.NewGuid().ToString().Substring(0, 8)}", // Generate a mock ref number
        SubmittedAt = DateTime.UtcNow
    };

    await quarterlyUpdatesCollection.ReplaceOneAsync(q => q.Id == id, quarterToSubmit);

    return Results.Ok(new
    {
        quarterToSubmit.Id,
        quarterToSubmit.BusinessId,
        quarterToSubmit.TaxYear,
        quarterToSubmit.QuarterName,
        quarterToSubmit.TaxableIncome,
        quarterToSubmit.AllowableExpenses,
        quarterToSubmit.NetProfit,
        quarterToSubmit.Status,
        quarterToSubmit.SubmissionDetails,
        Message = "Quarter submitted successfully."
    });
});

// Map Quarterly Data Endpoints
app.MapGet("/api/quarters", async (
    HttpContext httpContext, // Inject HttpContext to get headers
    ApplicationDbContext dbContext,
    IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
{
    // Get UserId from authenticated user's JWT token (mock version)
    var currentUserId = GetUserIdFromMockToken(httpContext.Request.Headers.Authorization.FirstOrDefault());
    if (string.IsNullOrEmpty(currentUserId))
    {
        return Results.Unauthorized();
    }

    // Get the user's business from SQL Server
    var business = await dbContext.Businesses
                                  .Where(b => b.UserId == currentUserId)
                                  .FirstOrDefaultAsync();

    if (business == null)
    {
        return Results.NotFound("No business found for the current user.");
    }

    // Get all quarters for this business from MongoDB
    var quarters = await quarterlyUpdatesCollection
                         .Find(q => q.BusinessId == business.Id)
                         .ToListAsync();

    if (!quarters.Any())
    {
        return Results.NotFound($"No quarterly updates found for business ID {business.Id}.");
    }

    return Results.Ok(quarters);
});

app.Run();
