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

public static class QuarterlyUpdateEndpoints
{
    public static void MapQuarterlyUpdateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/quarters", async (
            HttpContext httpContext,
            IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) => // Removed ApplicationDbContext, as business is already retrieved
        {
            // Retrieve currentUserId and business from HttpContext.Items set by the filter
            var currentUserId = httpContext.Items["currentUserId"] as string;
            var business = httpContext.Items["business"] as Business;

            if (string.IsNullOrEmpty(currentUserId)) return Results.Unauthorized(); // Should not happen
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

            var submittedQuarters = quarters.Where(q => q.Status == "SUBMITTED").ToList();

            decimal totalNetProfitSubmitted = 0.00m;
            // Define personal allowance and tax rate
            decimal personalAllowance = 12570.00m; // Example: UK personal allowance for 2023/24
            decimal basicTaxRate = 0.20m; // Example: 20% basic rate tax

            foreach (var quarter in quarters)
            {
                // Ensure NetProfit is calculated for all quarters before summing
                quarter.NetProfit = quarter.TaxableIncome - quarter.AllowableExpenses;

                if (quarter.Status == "SUBMITTED")
                {
                    totalNetProfitSubmitted += quarter.NetProfit;
                }
            }

            decimal cumulativeEstimatedTaxLiability = 0.00m;
            // Calculate tax liability based on total net profit from submitted quarters
            if (totalNetProfitSubmitted > personalAllowance)
            {
                decimal taxableAmount = totalNetProfitSubmitted - personalAllowance;
                // Apply a flat tax rate on the amount above the personal allowance
                cumulativeEstimatedTaxLiability = taxableAmount * basicTaxRate;
            }

            return Results.Ok(new QuartersResponse
            {
                Quarters = quarters.OrderBy(q => q.TaxYear).ThenBy(q => q.QuarterName).ToList(),
                TotalNetProfitSubmitted = totalNetProfitSubmitted,
                CumulativeEstimatedTaxLiability = cumulativeEstimatedTaxLiability
            });
        })
        .Produces<QuartersResponse>(StatusCodes.Status200OK) // Explicitly defines 200 OK response
        .Produces(StatusCodes.Status401Unauthorized)         // Explicitly defines 401 Unauthorized response
        .Produces(StatusCodes.Status404NotFound)             // Explicitly defines 404 Not Found response
        .AddEndpointFilter<AuthAndBusinessFilter>()
        .WithOpenApi(operation =>
        {
            operation.Summary = "Retrieve all fiscal quarters for the user's business.";
            operation.Description = "Returns a list of all quarterly updates, including calculated net profit for each, and cumulative financial summaries (total net profit from submitted quarters and estimated tax liability).";
            return operation; // FIX: Return the operation object
        });

        app.MapPut("/api/quarter/{id}", async (
            string id,
            QuarterlyUpdateRequest model,
            HttpContext httpContext,
            IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
        {
            // Retrieve currentUserId and business from HttpContext.Items set by the filter
            var currentUserId = httpContext.Items["currentUserId"] as string;
            // FIX: Explicitly declare 'business' as nullable to resolve CS8601 warning.
            Business? business = httpContext.Items["business"] as Business;

            if (string.IsNullOrEmpty(currentUserId)) return Results.Unauthorized(); // Should not happen
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

            if (quarterToUpdate.Status != "DRAFT")
            {
                return Results.BadRequest("Only quarters in 'DRAFT' status can be updated.");
            }

            quarterToUpdate.TaxableIncome = model.TaxableIncome;
            quarterToUpdate.AllowableExpenses = model.AllowableExpenses;
            quarterToUpdate.NetProfit = model.TaxableIncome - quarterToUpdate.AllowableExpenses;

            await quarterlyUpdatesCollection.ReplaceOneAsync(q => q.Id == id, quarterToUpdate);

            // Using the strongly typed DTO as indicated in the HANDOVER.md
            return Results.Ok(new QuarterUpdateResponse
            {
                Id = quarterToUpdate.Id,
                BusinessId = quarterToUpdate.BusinessId,
                TaxYear = quarterToUpdate.TaxYear,
                QuarterName = quarterToUpdate.QuarterName,
                TaxableIncome = quarterToUpdate.TaxableIncome,
                AllowableExpenses = quarterToUpdate.AllowableExpenses,
                NetProfit = quarterToUpdate.NetProfit,
                Status = quarterToUpdate.Status,
                Message = "Draft saved."
            });
        })
        .Produces<QuarterUpdateResponse>(StatusCodes.Status200OK) // Explicitly defines 200 OK response with the new DTO
        .Produces(StatusCodes.Status401Unauthorized)              // Explicitly defines 401 Unauthorized response
        .Produces(StatusCodes.Status404NotFound)                  // Explicitly defines 404 Not Found response
        .Produces(StatusCodes.Status400BadRequest)                // Explicitly defines 400 Bad Request response
        .AddEndpointFilter<AuthAndBusinessFilter>() // Apply the filter here!
        .WithOpenApi(operation =>
        {
            operation.Summary = "Update a specific quarterly update in 'DRAFT' status.";
            operation.Description = "Saves or updates the taxable income and allowable expenses for a quarterly update identified by its ID. Only quarters in 'DRAFT' status can be updated. Net Profit is automatically calculated.";
            return operation; // FIX: Return the operation object
        });

        app.MapPost("/api/quarter/{id}/submit", async (
            string id,
            HttpContext httpContext,
            IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
        {
            // Retrieve currentUserId and business from HttpContext.Items set by the filter
            var currentUserId = httpContext.Items["currentUserId"] as string;
            // FIX: Explicitly declare 'business' as nullable to resolve CS8601 warning.
            Business? business = httpContext.Items["business"] as Business;

            if (string.IsNullOrEmpty(currentUserId)) return Results.Unauthorized(); // Should not happen
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

            if (quarterToSubmit.Status != "DRAFT")
            {
                return Results.BadRequest("Only quarters in 'DRAFT' status can be submitted.");
            }

            quarterToSubmit.Status = "SUBMITTED";
            quarterToSubmit.SubmissionDetails = new SubmissionDetails
            {
                RefNumber = $"MTD-ACK-{Guid.NewGuid().ToString().Substring(0, 8)}",
                SubmittedAt = DateTime.UtcNow
            };

            await quarterlyUpdatesCollection.ReplaceOneAsync(q => q.Id == id, quarterToSubmit);

            // Using the strongly typed DTO as indicated in the HANDOVER.md
            return Results.Ok(new QuarterSubmissionResponse
            {
                Id = quarterToSubmit.Id,
                BusinessId = quarterToSubmit.BusinessId,
                TaxYear = quarterToSubmit.TaxYear,
                QuarterName = quarterToSubmit.QuarterName,
                TaxableIncome = quarterToSubmit.TaxableIncome,
                AllowableExpenses = quarterToSubmit.AllowableExpenses,
                NetProfit = quarterToSubmit.NetProfit,
                Status = quarterToSubmit.Status,
                SubmissionDetails = quarterToSubmit.SubmissionDetails,
                Message = "Quarter submitted successfully."
            });
        })
        .Produces<QuarterSubmissionResponse>(StatusCodes.Status200OK) // Explicitly defines 200 OK response with the new DTO
        .Produces(StatusCodes.Status401Unauthorized)                 // Explicitly defines 401 Unauthorized response
        .Produces(StatusCodes.Status404NotFound)                     // Explicitly defines 404 Not Found response
        .Produces(StatusCodes.Status400BadRequest)                   // Explicitly defines 400 Bad Request response
        .AddEndpointFilter<AuthAndBusinessFilter>() // Apply the filter here!
        .WithOpenApi(operation =>
        {
            operation.Summary = "Submit a quarterly update.";
            operation.Description = "Marks a specific quarterly update as 'SUBMITTED', generating a mock reference number and submission timestamp. Only quarters in 'DRAFT' status can be submitted.";
            return operation; // FIX: Return the operation object
        });
    }
}
