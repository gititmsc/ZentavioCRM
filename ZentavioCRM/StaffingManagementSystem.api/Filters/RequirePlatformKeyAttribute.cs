using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ZentavioCRM.Core.Common;

namespace ZentavioCRM.Api.Filters
{
    /// <summary>
    /// Guards the tenant-provisioning endpoints with a shared secret rather than the normal
    /// per-tenant JWT scheme — provisioning creates the FIRST user of a brand-new tenant, so
    /// there's no tenant or user to authenticate as yet. Compares the "X-Platform-Key" request
    /// header against configuration key "Platform:ProvisioningKey".
    ///
    /// This is a minimum-viable guard, not a real platform-admin identity system — replace it
    /// with proper platform-operator authentication before these endpoints are reachable from
    /// anywhere but a trusted internal network.
    /// </summary>
    public class RequirePlatformKeyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedKey = configuration["Platform:ProvisioningKey"];
            var providedKey = context.HttpContext.Request.Headers["X-Platform-Key"].ToString();

            if (string.IsNullOrEmpty(expectedKey) || !string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
            {
                context.Result = new ObjectResult(ApiResponse<object>.FailureResponse("Invalid or missing platform key."))
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                };
                return;
            }

            await next();
        }
    }
}
