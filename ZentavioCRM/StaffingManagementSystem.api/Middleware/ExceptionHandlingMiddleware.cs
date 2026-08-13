using ZentavioCRM.Core.Common;

namespace ZentavioCRM.Api.Middleware
{
    /// <summary>
    /// Catches unhandled exceptions so the response still flows through the normal pipeline (and
    /// therefore still carries the CORS headers UseCors already added) instead of the hosting layer
    /// generating a bare, header-less 500 that browsers report as a confusing CORS/network failure.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.FailureResponse("An unexpected error occurred."));
            }
        }
    }
}
