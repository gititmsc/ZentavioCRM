namespace ZentavioCRM.Core.Common
{
    /// <summary>
    /// Fixed IDs for platform-seeded rows (default company, default roles, admin user...).
    /// Shared between the Infrastructure seeder (which inserts these rows) and the Services
    /// layer (which needs to know, e.g., "which company is the current single tenant").
    /// Safe to reference from Core because it holds no EF/Infrastructure dependencies.
    /// </summary>
    public static class SeedIds
    {
        public static readonly Guid DefaultCompanyId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        public static readonly Guid DefaultDepartmentId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        public static readonly Guid AdminUserId = Guid.Parse("50000000-0000-0000-0000-000000000001");

        public static readonly Guid AdminRoleId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        public static readonly Guid SalesManagerRoleId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        public static readonly Guid SalesExecutiveRoleId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        public static readonly Guid SupportAgentRoleId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    }
}
