using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Infrastructure.Persistence.Seed
{
    /// <summary>
    /// Deterministic seed data applied via EF Core <c>HasData</c>. All IDs are fixed GUIDs
    /// (rather than <c>Guid.NewGuid()</c>) so migrations are reproducible and idempotent.
    /// The IDs themselves live in <see cref="SeedIds"/> (Core layer) so the Services layer
    /// can reference "the default company/roles" without depending on Infrastructure.
    /// </summary>
    public static class PlatformSeedData
    {
        /// <summary>Fixed GUID per permission code, referenced by both the Permission rows and the RolePermission grants.</summary>
        private static readonly Dictionary<string, Guid> PermissionIds = new()
        {
            [PermissionCodes.DepartmentsView] = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            [PermissionCodes.DepartmentsManage] = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            [PermissionCodes.TerritoriesView] = Guid.Parse("10000000-0000-0000-0000-000000000020"),
            [PermissionCodes.TerritoriesManage] = Guid.Parse("10000000-0000-0000-0000-000000000021"),
            [PermissionCodes.UsersView] = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            [PermissionCodes.UsersManage] = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            [PermissionCodes.RolesView] = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            [PermissionCodes.RolesManage] = Guid.Parse("10000000-0000-0000-0000-000000000006"),
            [PermissionCodes.CustomersView] = Guid.Parse("10000000-0000-0000-0000-000000000007"),
            [PermissionCodes.CustomersCreate] = Guid.Parse("10000000-0000-0000-0000-000000000008"),
            [PermissionCodes.CustomersEdit] = Guid.Parse("10000000-0000-0000-0000-000000000009"),
            [PermissionCodes.CustomersDelete] = Guid.Parse("10000000-0000-0000-0000-00000000000a"),
            [PermissionCodes.LeadsView] = Guid.Parse("10000000-0000-0000-0000-00000000000b"),
            [PermissionCodes.LeadsCreate] = Guid.Parse("10000000-0000-0000-0000-00000000000c"),
            [PermissionCodes.LeadsEdit] = Guid.Parse("10000000-0000-0000-0000-00000000000d"),
            [PermissionCodes.LeadsDelete] = Guid.Parse("10000000-0000-0000-0000-00000000000e"),
            [PermissionCodes.LeadsAssign] = Guid.Parse("10000000-0000-0000-0000-00000000000f"),
            [PermissionCodes.LeadsConvert] = Guid.Parse("10000000-0000-0000-0000-000000000010"),
            [PermissionCodes.OpportunitiesView] = Guid.Parse("10000000-0000-0000-0000-000000000011"),
            [PermissionCodes.OpportunitiesCreate] = Guid.Parse("10000000-0000-0000-0000-000000000012"),
            [PermissionCodes.OpportunitiesEdit] = Guid.Parse("10000000-0000-0000-0000-000000000013"),
            [PermissionCodes.OpportunitiesDelete] = Guid.Parse("10000000-0000-0000-0000-000000000014"),
            [PermissionCodes.OpportunitiesAssign] = Guid.Parse("10000000-0000-0000-0000-000000000015"),
            [PermissionCodes.QuotationsView] = Guid.Parse("10000000-0000-0000-0000-000000000016"),
            [PermissionCodes.QuotationsCreate] = Guid.Parse("10000000-0000-0000-0000-000000000017"),
            [PermissionCodes.QuotationsEdit] = Guid.Parse("10000000-0000-0000-0000-000000000018"),
            [PermissionCodes.QuotationsDelete] = Guid.Parse("10000000-0000-0000-0000-000000000019"),
            [PermissionCodes.QuotationsAssign] = Guid.Parse("10000000-0000-0000-0000-00000000001a"),
            [PermissionCodes.SalesOrdersView] = Guid.Parse("10000000-0000-0000-0000-00000000001b"),
            [PermissionCodes.SalesOrdersCreate] = Guid.Parse("10000000-0000-0000-0000-00000000001c"),
            [PermissionCodes.SalesOrdersEdit] = Guid.Parse("10000000-0000-0000-0000-00000000001d"),
            // 10000000-0000-0000-0000-00000000001e was SalesOrders.Delete — retired, no delete feature exists for Sales Orders.
            [PermissionCodes.SalesOrdersAssign] = Guid.Parse("10000000-0000-0000-0000-00000000001f"),
        };

        /// <summary>Fixed point in time used for every seeded "CreatedAtUtc" column so migrations stay deterministic.</summary>
        private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>PBKDF2 hash (100,000 iterations, SHA-256) of the default admin password "Admin@123". Change on first login.</summary>
        private const string AdminPasswordHash = "100000.gH7ZXrg9PRCjHhBJrL8z0g==.O3iZBLoIKRGP09JaxOc7dNAqvM8orTypo6ti+p3wQOs=";

        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().HasData(new Company
            {
                Id = SeedIds.DefaultCompanyId,
                Name = "My Company",
                DefaultCurrency = "USD",
                TimeZone = "UTC",
                CreatedAtUtc = SeedTimestamp,
            });

            modelBuilder.Entity<Department>().HasData(new Department
            {
                Id = SeedIds.DefaultDepartmentId,
                CompanyId = SeedIds.DefaultCompanyId,
                Name = "Sales",
                IsActive = true,
                CreatedAtUtc = SeedTimestamp,
            });

            SeedPermissions(modelBuilder);
            SeedRoles(modelBuilder);
            SeedRolePermissions(modelBuilder);

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = SeedIds.AdminUserId,
                EmployeeCode = "EMP-0001",
                FirstName = "System",
                LastName = "Administrator",
                Email = "admin@zentaviocrm.com",
                PasswordHash = AdminPasswordHash,
                RoleId = SeedIds.AdminRoleId,
                DepartmentId = SeedIds.DefaultDepartmentId,
                IsActive = true,
                CreatedAtUtc = SeedTimestamp,
            });
        }

        private static void SeedPermissions(ModelBuilder modelBuilder)
        {
            var permissions = PermissionCodes.ByModule.SelectMany(module => module.Value.Select(code => new Permission
            {
                Id = PermissionIds[code],
                Code = code,
                Name = code.Split('.')[1],
                Module = module.Key,
            }));

            modelBuilder.Entity<Permission>().HasData(permissions);
        }

        private static void SeedRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = SeedIds.AdminRoleId, Name = "Administrator", Description = "Full access to every module and setting.", IsSystemRole = true, CreatedAtUtc = SeedTimestamp },
                new Role { Id = SeedIds.SalesManagerRoleId, Name = "Sales Manager", Description = "Manages the sales team's customers and lead pipeline.", IsSystemRole = true, CreatedAtUtc = SeedTimestamp },
                new Role { Id = SeedIds.SalesExecutiveRoleId, Name = "Sales Executive", Description = "Works leads and customers assigned to them.", IsSystemRole = true, CreatedAtUtc = SeedTimestamp },
                new Role { Id = SeedIds.SupportAgentRoleId, Name = "Support Agent", Description = "Read-only access to customers and leads.", IsSystemRole = true, CreatedAtUtc = SeedTimestamp }
            );
        }

        private static void SeedRolePermissions(ModelBuilder modelBuilder)
        {
            var grants = new List<RolePermission>();

            // Administrator — every permission in the system.
            grants.AddRange(PermissionCodes.All.Select(code => new RolePermission { RoleId = SeedIds.AdminRoleId, PermissionId = PermissionIds[code] }));

            // Sales Manager — full Customers/Leads/Opportunities/Quotations/SalesOrders, plus visibility into Departments/Users.
            string[] salesManagerCodes =
            [
                PermissionCodes.DepartmentsView,
                PermissionCodes.UsersView,
                PermissionCodes.CustomersView, PermissionCodes.CustomersCreate, PermissionCodes.CustomersEdit, PermissionCodes.CustomersDelete,
                PermissionCodes.LeadsView, PermissionCodes.LeadsCreate, PermissionCodes.LeadsEdit, PermissionCodes.LeadsDelete, PermissionCodes.LeadsAssign, PermissionCodes.LeadsConvert,
                PermissionCodes.OpportunitiesView, PermissionCodes.OpportunitiesCreate, PermissionCodes.OpportunitiesEdit, PermissionCodes.OpportunitiesDelete, PermissionCodes.OpportunitiesAssign,
                PermissionCodes.QuotationsView, PermissionCodes.QuotationsCreate, PermissionCodes.QuotationsEdit, PermissionCodes.QuotationsDelete, PermissionCodes.QuotationsAssign,
                PermissionCodes.SalesOrdersView, PermissionCodes.SalesOrdersCreate, PermissionCodes.SalesOrdersEdit, PermissionCodes.SalesOrdersAssign,
            ];
            grants.AddRange(salesManagerCodes.Select(code => new RolePermission { RoleId = SeedIds.SalesManagerRoleId, PermissionId = PermissionIds[code] }));

            // Sales Executive — day-to-day CRUD, no deletes.
            string[] salesExecutiveCodes =
            [
                PermissionCodes.CustomersView, PermissionCodes.CustomersCreate, PermissionCodes.CustomersEdit,
                PermissionCodes.LeadsView, PermissionCodes.LeadsCreate, PermissionCodes.LeadsEdit, PermissionCodes.LeadsAssign, PermissionCodes.LeadsConvert,
                PermissionCodes.OpportunitiesView, PermissionCodes.OpportunitiesCreate, PermissionCodes.OpportunitiesEdit, PermissionCodes.OpportunitiesAssign,
                PermissionCodes.QuotationsView, PermissionCodes.QuotationsCreate, PermissionCodes.QuotationsEdit, PermissionCodes.QuotationsAssign,
                PermissionCodes.SalesOrdersView, PermissionCodes.SalesOrdersCreate, PermissionCodes.SalesOrdersEdit, PermissionCodes.SalesOrdersAssign,
            ];
            grants.AddRange(salesExecutiveCodes.Select(code => new RolePermission { RoleId = SeedIds.SalesExecutiveRoleId, PermissionId = PermissionIds[code] }));

            // Support Agent — read-only.
            string[] supportAgentCodes =
            [
                PermissionCodes.CustomersView, PermissionCodes.LeadsView, PermissionCodes.OpportunitiesView,
                PermissionCodes.QuotationsView, PermissionCodes.SalesOrdersView,
            ];
            grants.AddRange(supportAgentCodes.Select(code => new RolePermission { RoleId = SeedIds.SupportAgentRoleId, PermissionId = PermissionIds[code] }));

            modelBuilder.Entity<RolePermission>().HasData(grants);
        }
    }
}
