using Microsoft.Extensions.DependencyInjection;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services.Extensions
{
    /// <summary>
    /// Registers Service-layer (business logic) services with the DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ILeadService, LeadService>();
            services.AddScoped<IOpportunityService, OpportunityService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
