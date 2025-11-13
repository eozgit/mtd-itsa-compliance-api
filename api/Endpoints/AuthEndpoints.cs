
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
        // Format: mock-jwt-token-for-<userId>-<userName>-<email>
        // userId can contain hyphens, userName and email should not.
        return $"mock-jwt-token-for-{userId}-{userName}-{email}";
    }

    // Helper: Extract UserId from the mock Authorization header
    // Marked as public static for use by other endpoint modules
    public static string? GetUserIdFromMockToken(string? authorizationHeader)
    {
        // Console.WriteLine($"DEBUG: GetUserIdFromMockToken received: {authorizationHeader}"); // DIAGNOSTIC
        const string prefix = "Bearer mock-jwt-token-for-";

        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith(prefix))
        {
            // Console.WriteLine("DEBUG: GetUserIdFromMockToken - Header is null/empty or doesn't start with expected prefix."); // DIAGNOSTIC
            return null;
        }

        var tokenPayload = authorizationHeader.Substring(prefix.Length);
        // Console.WriteLine($"DEBUG: GetUserIdFromMockToken - Extracted token payload: {tokenPayload}"); // DIAGNOSTIC

        var lastHyphenIndex = tokenPayload.LastIndexOf('-');
        if (lastHyphenIndex == -1) return null; // Missing email

        var secondLastHyphenIndex = tokenPayload.LastIndexOf('-', lastHyphenIndex - 1);
        if (secondLastHyphenIndex == -1) return null; // Missing userName

        var userId = tokenPayload.Substring(0, secondLastHyphenIndex);

        // Console.WriteLine($"DEBUG: GetUserIdFromMockToken - Parsed UserId: '{userId}'"); // DIAGNOSTIC

        return userId;
    }
}
