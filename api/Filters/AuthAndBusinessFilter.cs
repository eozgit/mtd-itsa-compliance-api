
using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using static api.Endpoints.AuthEndpoints; // For GetUserIdFromMockToken
using Microsoft.AspNetCore.Http; // NEW: Required for EndpointFilterInvocationContext

namespace api.Filters;

public class AuthAndBusinessFilter : IEndpointFilter
{
    private readonly ApplicationDbContext _dbContext;

    public AuthAndBusinessFilter(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // CORRECTED: Changed EndpointFilterContext to EndpointFilterInvocationContext
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var authorizationHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();

        var currentUserId = GetUserIdFromMockToken(authorizationHeader);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Results.Unauthorized();
        }

        // Store UserId in HttpContext.Items for later retrieval by the endpoint handler
        httpContext.Items["currentUserId"] = currentUserId;

        // Check if the endpoint requires a business object (most do after login)
        var business = await _dbContext.Businesses
                                       .Where(b => b.UserId == currentUserId)
                                       .FirstOrDefaultAsync();

        // Always store, even if null, so endpoints can check for null
        httpContext.Items["business"] = business;

        return await next(context);
    }
}
