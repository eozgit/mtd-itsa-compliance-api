
using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder; // Required for IEndpointRouteBuilder extension
using Microsoft.AspNetCore.Http; // Required for Results, HttpContext

namespace api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
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
    }
    // Helper for generating a mock JWT token (replace with real JWT implementation later)
    private static string GenerateMockJwtToken(string userId, string userName, string email)
    {
        // MODIFIED: Use '|' as separator to avoid clashes with hyphens in GUIDs/emails
        return $"mock-jwt-token-for-{userId}|{userName}|{email}";
    }

    // Helper: Extract UserId from the mock Authorization header
    public static string? GetUserIdFromMockToken(string? authorizationHeader)
    {
        const string prefix = "Bearer mock-jwt-token-for-";

        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith(prefix))
        {
            return null;
        }

        var tokenPayload = authorizationHeader.Substring(prefix.Length);
        // MODIFIED: Split by '|' instead of '-'
        var parts = tokenPayload.Split('|');

        if (parts.Length != 3) // Expecting userId, userName, email
        {
            return null; // Invalid token format
        }

        var userId = parts[0]; // The first part is the userId
        return userId;
    }

}
