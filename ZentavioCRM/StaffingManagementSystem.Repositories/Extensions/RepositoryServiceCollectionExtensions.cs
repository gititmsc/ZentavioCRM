using Microsoft.Extensions.DependencyInjection;
using ZentavioCRM.Core.Interfaces;
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
            // Not a per-entity repository, but lives here because it needs the same direct
            // AppDbContext access every repository in this project already has — see
            // IUsageLimitService's remarks for why it isn't in the Services layer instead.
            services.AddScoped<IUsageLimitService, UsageLimitService>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<ITerritoryRepository, TerritoryRepository>();
            services.AddScoped<IUserDelegationRepository, UserDelegationRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ILeadRepository, LeadRepository>();
            services.AddScoped<ILeadScoringSettingsRepository, LeadScoringSettingsRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IMergeRepository, MergeRepository>();
            services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

            return services;
        }
    }
}
