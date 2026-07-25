using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Infrastructure.Persistence.Seed;

namespace ZentavioCRM.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core database context for ZentavioCRM.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<Company> Companies => Set<Company>();

        public DbSet<Department> Departments => Set<Department>();

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<Permission> Permissions => Set<Permission>();

        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<ContactPerson> ContactPersons => Set<ContactPerson>();

        public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

        public DbSet<Lead> Leads => Set<Lead>();

        public DbSet<Opportunity> Opportunities => Set<Opportunity>();

        public DbSet<Activity> Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // TenantConfiguration lives in this same assembly (Persistence/Configurations) for
            // PlatformDbContext to use, so the blanket scan above picks it up here too — exclude
            // it explicitly. Tenant registry rows belong only in the Platform database; a tenant's
            // own database (built from TenantSchema.sql) never has a Tenants table.
            modelBuilder.Ignore<Tenant>();

            PlatformSeedData.Seed(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }
    }
}
