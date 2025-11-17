
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson; // ADDED: Required for ObjectId.GenerateNewId()
using MongoDB.Driver;
using api.Filters;
using Microsoft.AspNetCore.OpenApi;
using System;
using System.Collections.Generic; // Added for List<QuarterlyUpdate>

namespace api.Endpoints;

public static class BusinessEndpoints
{
    public static void MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/business", async (
            BusinessRequest request,
            HttpContext httpContext,
            ApplicationDbContext dbContext,
            IMongoCollection<QuarterlyUpdate> quarterlyUpdatesCollection) =>
        {
            var currentUserId = httpContext.Items["currentUserId"] as string;

            if (string.IsNullOrEmpty(currentUserId)) return Results.Unauthorized();

            // 1. Create and Save Business to SQL
            var business = new Business
            {
                UserId = currentUserId,
                Name = request.Name,
                StartDate = request.StartDate.Date, // Ensure date only
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Businesses.Add(business);
            await dbContext.SaveChangesAsync(); // business.Id is populated here after saving to SQL

            // 2. Initialize 4 Fiscal Quarters and Save to MongoDB
            var quarterlyUpdates = new List<QuarterlyUpdate>();
            var initialStartDate = business.StartDate;

            // Determine the tax year start for the given business start date
            // MTD fiscal year starts on April 6th
            int taxYearStartYear = initialStartDate.Month < 4 || (initialStartDate.Month == 4 && initialStartDate.Day < 6)
                                 ? initialStartDate.Year - 1
                                 : initialStartDate.Year;
            if (initialStartDate.Month == 4 && initialStartDate.Day < 6)
            {
                // If StartDate is before April 6th in its year, the tax year began the previous April 6th.
                // e.g., if StartDate is 2025-03-01, the tax year is 2024/25.
                // if StartDate is 2025-04-01, the tax year is 2024/25.
                // If StartDate is 2025-04-06, the tax year is 2025/26.
                // This logic correctly aligns the start of the tax year.
                // A simpler way: if the StartDate is on or after April 6th, it's the current year's tax year.
                // Otherwise, it's the previous year's tax year.
                taxYearStartYear = initialStartDate.Year;
            }
            else
            {
                taxYearStartYear = initialStartDate.Year;
            }

            // A more robust way to find the fiscal year start:
            DateTime fiscalYearStart;
            if (initialStartDate.Month < 4 || (initialStartDate.Month == 4 && initialStartDate.Day < 6))
            {
                fiscalYearStart = new DateTime(initialStartDate.Year - 1, 4, 6);
            }
            else
            {
                fiscalYearStart = new DateTime(initialStartDate.Year, 4, 6);
            }

            string taxYear = $"{fiscalYearStart.Year}/{fiscalYearStart.Year + 1 - 2000}"; // e.g., "2025/26"

            // Calculate quarters and add to list
            for (int i = 0; i < 4; i++)
            {
                // Each quarter is approx 3 months (92 days for MTD)
                // For simplicity, using a fixed start day relative to fiscal year start.
                // Actual MTD quarters are:
                // Q1: April 6 - July 5
                // Q2: July 6 - October 5
                // Q3: October 6 - January 5
                // Q4: January 6 - April 5 (of next year)

                // The backend just needs to record the quarter name and tax year.
                // The frontend can determine the exact dates for display.
                string quarterName = $"Q{i + 1}";

                quarterlyUpdates.Add(new QuarterlyUpdate
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    BusinessId = business.Id, // Correctly assigning the SQL Business ID
                    TaxYear = taxYear,
                    QuarterName = quarterName,
                    Status = "DRAFT",
                    TaxableIncome = 0.00m,
                    AllowableExpenses = 0.00m,
                    NetProfit = 0.00m // Will be calculated on GET or PUT
                });
            }

            await quarterlyUpdatesCollection.InsertManyAsync(quarterlyUpdates);

            return Results.Created($"/api/business/{business.Id}", new BusinessResponse(business.Id, business.Name));
        })
        .Produces<BusinessResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .AddEndpointFilter<AuthAndBusinessFilter>()
        .WithOpenApi(operation =>
        {
            operation.Summary = "Register a new business for the authenticated user and initialize 4 fiscal quarters.";
            operation.Description = "This endpoint creates a new business entry linked to the current user and automatically sets up four initial 'DRAFT' quarterly update records in MongoDB for the relevant tax year based on the provided start date.";
            return operation;
        });
    }
}
