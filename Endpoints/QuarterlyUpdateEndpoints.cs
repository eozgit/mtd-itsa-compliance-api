
using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder; // Required for IEndpointRouteBuilder extension
using Microsoft.AspNetCore.Http; // Required for Results, HttpContext
using MongoDB.Driver; // For IMongoCollection

// Import the AuthEndpoints for the helper function
using static api.Endpoints.AuthEndpoints;

namespace api.Endpoints;


public static class QuarterlyUpdateEndpoints
{
    public static void MapQuarterlyUpdateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/quarters", async (
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

            var quarters = await quarterlyUpdatesCollection
                                 .Find(q => q.BusinessId == business.Id)
                                 .ToListAsync();

            if (!quarters.Any())
            {
                return Results.NotFound($"No quarterly updates found for business ID {business.Id}.");
            }

            // Calculate Cumulative Estimated Tax Liability
            const decimal taxRate = 0.20m; // Example: 20% tax rate
            var submittedQuarters = quarters.Where(q => q.Status == "SUBMITTED").ToList();
            var totalNetProfitSubmitted = submittedQuarters.Sum(q => q.NetProfit);
            var cumulativeTaxLiability = totalNetProfitSubmitted * taxRate;

            return Results.Ok(new QuartersResponse
            {
                Quarters = quarters,
                TotalNetProfitSubmitted = totalNetProfitSubmitted,
                CumulativeEstimatedTaxLiability = cumulativeTaxLiability
            });
        });

        // ...existing code...
    }
}
