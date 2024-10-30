using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BonPortalBackend.Middleware;

public class DatabaseExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseExceptionMiddleware> _logger;

    public DatabaseExceptionMiddleware(RequestDelegate next, ILogger<DatabaseExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (DbUpdateException ex)
    {
        await HandleDatabaseExceptionAsync(context, ex);
    }
    catch (SqlException ex)
    {
        await HandleDatabaseExceptionAsync(context, ex);
    }
}

    private async Task HandleDatabaseExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Database error occurred");
        
        context.Response.Redirect("/Home/Error?isDatabaseError=true");
    }
}

// Extension method for easy middleware registration
public static class DatabaseExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseDatabaseExceptionHandler(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DatabaseExceptionMiddleware>();
    }
}