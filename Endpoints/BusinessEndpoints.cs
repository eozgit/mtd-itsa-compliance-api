
using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder; // Required for IEndpointRouteBuilder extension
using Microsoft.AspNetCore.Http; // Required for Results, HttpContext
using MongoDB.Driver; // For IMongoCollection

// Import the AuthEndpoints for the helper function
using static api.Endpoints.AuthEndpoints;

namespace api.Endpoints;

public static class BusinessEndpoints
{
    public static void MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/business", async (
            HttpContext httpContext,
            BusinessRequest model,
            ApplicationDbContext dbContext,
            IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
        {
            var currentUserId = GetUserIdFromMockToken(httpContext.Request.Headers.Authorization.FirstOrDefault());
            // Console.WriteLine($"DEBUG: /api/business - currentUserId: {currentUserId}"); // DIAGNOSTIC
            if (string.IsNullOrEmpty(currentUserId))
            {
                // Console.WriteLine("DEBUG: /api/business - Unauthorized: currentUserId is null or empty."); // DIAGNOSTIC
                return Results.Unauthorized();
            }

            var userExists = await dbContext.Users.AnyAsync(u => u.Id == currentUserId);
            // Console.WriteLine($"DEBUG: /api/business - userExists in DB for ID '{currentUserId}': {userExists}"); // DIAGNOSTIC
            if (!userExists)
            {
                // Console.WriteLine("DEBUG: /api/business - Unauthorized: Token user not found in DB."); // DIAGNOSTIC
                return Results.Unauthorized();
            }

            if (await dbContext.Businesses.AnyAsync(b => b.UserId == currentUserId))
            {
                return Results.Conflict("User already has a registered business.");
            }

            var newBusiness = new Business
            {
                UserId = currentUserId,
                Name = model.Name,
                StartDate = model.StartDate,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Businesses.Add(newBusiness);
            await dbContext.SaveChangesAsync();
            // Console.WriteLine($"SQL: Created Business with ID: {newBusiness.Id}");

            var quarters = GenerateFiscalQuarters(newBusiness.StartDate, newBusiness.Id);
            // Console.WriteLine($"MongoDB: Generated {quarters.Count} quarters for Business ID: {newBusiness.Id}");

            if (quarters.Any())
            {
                await quarterlyUpdatesCollection.InsertManyAsync(quarters);
                // Console.WriteLine($"MongoDB: Successfully inserted {quarters.Count} quarterly updates.");
            }
            else
            {
                // Console.WriteLine($"MongoDB: No quarters were generated to insert for Business ID: {newBusiness.Id}.");
            }

            return Results.Created($"/api/business/{newBusiness.Id}", new BusinessResponse(newBusiness.Id, newBusiness.Name));
        });
    }

    // Helper for generating fiscal quarters (e.g., for MTD ITSA, starting April 6th)
    private static List<QuarterlyUpdate> GenerateFiscalQuarters(DateTime startDate, int businessId)
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
        // Console.WriteLine($"DEBUG: GenerateFiscalQuarters - StartDate: {startDate:yyyy-MM-dd}, FiscalYearStart: {fiscalYearStart:yyyy-MM-dd}"); // DIAGNOSTIC LOG

        for (int i = 0; i < 4; i++)
        {
            var quarterStartDate = fiscalYearStart.AddMonths(i * 3);

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
                NetProfit = 0.00m // Calculated later or on retrieval for simplicity here
            });
            // Console.WriteLine($"DEBUG: Added quarter {quarterName} for {taxYear} with BusinessId {businessId}"); // DIAGNOSTIC LOG
        }
        return quarters;
    }
}
