// eort PATH="$PATH:/home/enis/.dotnet/tools"
using api.Models;
using api.Data; // Add this using directive for ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Add this for DbContext options
using MongoDB.Driver; // Add this for MongoDB types

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();

// Configure SQL Server with Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
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

// Placeholder for in-memory user store for mock authentication (REMOVING THIS SOON)
// For now, it will be replaced by direct DB calls in the endpoints.
// The 'users' list is no longer needed.

// Map authentication endpoints
app.MapPost("/api/auth/register", async (RegisterRequest model, ApplicationDbContext dbContext) => // Inject DbContext
{
    // Check for existing user in the database
    if (await dbContext.Users.AnyAsync(u => u.Email == model.Email))
    {
        return Results.Conflict("User with this email already exists.");
    }

    var userId = Guid.NewGuid().ToString();
    // Use 'Id' property instead of 'UserId'
    var newUser = new User { Id = userId, Email = model.Email, UserName = model.UserName, PasswordHash = model.Password };

    // Add new user to database
    dbContext.Users.Add(newUser);
    await dbContext.SaveChangesAsync(); // Save changes asynchronously

    // Generate mock JWT token
    var token = GenerateMockJwtToken(userId, model.UserName, model.Email);

    return Results.Ok(new AuthResponse(userId, model.UserName, token));
});

app.MapPost("/api/auth/login", async (LoginRequest model, ApplicationDbContext dbContext) => // Inject DbContext
{
    // Validate user against the database
    // Use 'Id' property when comparing
    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == model.Password);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    // Generate mock JWT token
    // Use 'Id' property of the user object
    var token = GenerateMockJwtToken(user.Id, user.UserName, user.Email);

    // Use 'Id' property of the user object for AuthResponse
    return Results.Ok(new AuthResponse(user.Id, user.UserName, token));
});


// Helper for generating a mock JWT token (replace with real JWT implementation later)
string GenerateMockJwtToken(string userId, string userName, string email)
{
    // In a real application, you would use System.IdentityModel.Tokens.Jwt
    // and configure signing credentials, audience, issuer, etc.
    // For now, this is a simple placeholder.
    return $"mock-jwt-token-for-{userId}-{userName}-{email}";
}

app.Run();
