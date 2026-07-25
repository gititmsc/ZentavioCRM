using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Infrastructure.Persistence.Configurations;

namespace ZentavioCRM.Infrastructure.Persistence
{
    /// <summary>
    /// The Platform (master) database — the tenant registry only. Never contains any tenant's
    /// application data. There is exactly one of these for the whole SaaS deployment, versus
    /// one <see cref="AppDbContext"/> database per tenant.
    ///
    /// Deliberately does NOT use ApplyConfigurationsFromAssembly: this project's assembly also
    /// holds every tenant-side IEntityTypeConfiguration (UserConfiguration, LeadConfiguration...),
    /// and a blanket scan would pull all of those tables into this database's model too. Only the
    /// one configuration this context actually owns is applied, explicitly.
    /// </summary>
    public class PlatformDbContext : DbContext
    {
        public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TenantConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
}
