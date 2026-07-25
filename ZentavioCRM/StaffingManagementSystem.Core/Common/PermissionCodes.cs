namespace ZentavioCRM.Core.Common
{
    /// <summary>
    /// Canonical list of permission codes. Used both to seed the <c>Permissions</c> table
    /// and to declare authorization policies in the Api layer, so a typo cannot silently
    /// create a policy that never matches a real permission.
    /// </summary>
    public static class PermissionCodes
    {
        /// <summary>Claim type used to carry granted permission codes in the JWT.</summary>
        public const string ClaimType = "permission";

        public const string DepartmentsView = "Departments.View";
        public const string DepartmentsManage = "Departments.Manage";

        public const string UsersView = "Users.View";
        public const string UsersManage = "Users.Manage";

        public const string RolesView = "Roles.View";
        public const string RolesManage = "Roles.Manage";

        public const string CustomersView = "Customers.View";
        public const string CustomersCreate = "Customers.Create";
        public const string CustomersEdit = "Customers.Edit";
        public const string CustomersDelete = "Customers.Delete";

        public const string LeadsView = "Leads.View";
        public const string LeadsCreate = "Leads.Create";
        public const string LeadsEdit = "Leads.Edit";
        public const string LeadsDelete = "Leads.Delete";
        public const string LeadsAssign = "Leads.Assign";
        public const string LeadsConvert = "Leads.Convert";

        public const string OpportunitiesView = "Opportunities.View";
        public const string OpportunitiesCreate = "Opportunities.Create";
        public const string OpportunitiesEdit = "Opportunities.Edit";
        public const string OpportunitiesDelete = "Opportunities.Delete";
        public const string OpportunitiesAssign = "Opportunities.Assign";

        /// <summary>All codes, keyed by module, used by the seeder and the admin role grant.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> ByModule = new Dictionary<string, string[]>
        {
            ["Departments"] = [DepartmentsView, DepartmentsManage],
            ["Users"] = [UsersView, UsersManage],
            ["Roles"] = [RolesView, RolesManage],
            ["Customers"] = [CustomersView, CustomersCreate, CustomersEdit, CustomersDelete],
            ["Leads"] = [LeadsView, LeadsCreate, LeadsEdit, LeadsDelete, LeadsAssign, LeadsConvert],
            ["Opportunities"] = [OpportunitiesView, OpportunitiesCreate, OpportunitiesEdit, OpportunitiesDelete, OpportunitiesAssign],
        };

        public static IEnumerable<string> All => ByModule.Values.SelectMany(codes => codes);
    }
}
