using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Email;
using ZentavioCRM.Infrastructure.Multitenancy;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Infrastructure.Security;

namespace ZentavioCRM.Infrastructure.Extensions
{
    /// <summary>
    /// Registers Infrastructure-layer services (DbContexts, tenancy, security utilities) with the DI container.
    /// </summary>
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<TenancySettings>(configuration.GetSection(TenancySettings.SectionName));
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
            services.Configure<FrontendSettings>(configuration);

            // Scoped per request. TenantResolutionMiddleware populates this before any controller
            // action runs; AppDbContext's options factory below reads it to pick the right database.
            services.AddScoped<ITenantContext, TenantContext>();

            // AppDbContext's connection string is resolved lazily, per request, from whichever
            // tenant TenantResolutionMiddleware already identified earlier in the same request's
            // pipeline — this is what makes "one database per tenant" work without every
            // repository/service needing to know tenancy exists.
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var tenantContext = serviceProvider.GetRequiredService<ITenantContext>();
                var tenancySettings = serviceProvider.GetRequiredService<IOptions<TenancySettings>>().Value;

                var connectionString = tenantContext.ConnectionString;

                if (connectionString is null && !string.IsNullOrWhiteSpace(tenancySettings.DefaultTenantConnectionStringName))
                {
                    connectionString = configuration.GetConnectionString(tenancySettings.DefaultTenantConnectionStringName);
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "No tenant could be resolved for this request (missing/unknown X-Tenant header or subdomain) " +
                        "and no Tenancy:DefaultTenantConnectionStringName fallback is configured.");
                }

                options.UseSqlServer(connectionString);
            });

            // The Platform database is singular and shared — never tenant-scoped.
            services.AddDbContext<PlatformDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("PlatformDb")));

            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<ISecureTokenGenerator, SecureTokenGenerator>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

            return services;
        }
    }
}
