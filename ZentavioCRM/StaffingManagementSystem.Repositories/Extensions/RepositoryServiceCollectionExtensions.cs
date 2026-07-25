using Microsoft.Extensions.DependencyInjection;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories.Extensions
{
    /// <summary>
    /// Registers Repository-layer services with the DI container.
    /// </summary>
    public static class RepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ILeadRepository, LeadRepository>();
            services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();

            return services;
        }
    }
}
