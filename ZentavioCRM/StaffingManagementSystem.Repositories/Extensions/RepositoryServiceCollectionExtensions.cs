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
            services.AddScoped<ITerritoryRepository, TerritoryRepository>();
            services.AddScoped<IUserDelegationRepository, UserDelegationRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ILeadRepository, LeadRepository>();
            services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();

            return services;
        }
    }
}
