
using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder; // Required for IEndpointRouteBuilder extension
using Microsoft.AspNetCore.Http; // Required for Results, HttpContext
using MongoDB.Driver; // For IMongoCollection
using api.Filters; // NEW: Import the Filters namespace
using Microsoft.AspNetCore.OpenApi; // NEW: Required for WithOpenApi extension

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
            // Retrieve currentUserId from HttpContext.Items set by the filter
            var currentUserId = httpContext.Items["currentUserId"] as string;
            // The filter already handles Unauthorized, so this check is mostly for type safety/nullability
            if (string.IsNullOrEmpty(currentUserId)) return Results.Unauthorized(); // Should ideally not happen if filter passed

            var existingBusiness = httpContext.Items["business"] as Business;

            if (existingBusiness != null)
            {
                return Results.Conflict("User already has a registered business.");
            }

            // The user existence check can be removed because the filter already ensures currentUserId exists and is linked to business.
            // If `business` is null here, it means the user exists but has no business, which is the expected state for registration.

            var newBusiness = new Business
            {
                UserId = currentUserId,
                Name = model.Name,
                StartDate = model.StartDate,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Businesses.Add(newBusiness);
            await dbContext.SaveChangesAsync();

            var quarters = GenerateFiscalQuarters(newBusiness.StartDate, newBusiness.Id);

            if (quarters.Any())
            {
                await quarterlyUpdatesCollection.InsertManyAsync(quarters);
            }

            return Results.Created($"/api/business/{newBusiness.Id}", new BusinessResponse(newBusiness.Id, newBusiness.Name));
        })
        .Produces<BusinessResponse>(StatusCodes.Status201Created) // Explicitly defines 201 Created response
        .Produces(StatusCodes.Status401Unauthorized) // Explicitly defines 401 Unauthorized response
        .Produces(StatusCodes.Status409Conflict)     // Explicitly defines 409 Conflict response
        .AddEndpointFilter<AuthAndBusinessFilter>()  // Apply the filter here!
        .WithOpenApi(); // Ensures these definitions are included in the OpenAPI spec
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
