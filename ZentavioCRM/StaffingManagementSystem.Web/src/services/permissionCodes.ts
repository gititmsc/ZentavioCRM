/**
 * Mirrors ZentavioCRM.Core.Common.PermissionCodes on the backend. Kept as plain string
 * constants (not an enum) so they serialize identically to the "permission" claims in the JWT.
 */
export const PermissionCodes = {
  DepartmentsView: "Departments.View",
  DepartmentsManage: "Departments.Manage",

  UsersView: "Users.View",
  UsersManage: "Users.Manage",

  RolesView: "Roles.View",
  RolesManage: "Roles.Manage",

  CustomersView: "Customers.View",
  CustomersCreate: "Customers.Create",
  CustomersEdit: "Customers.Edit",
  CustomersDelete: "Customers.Delete",

  LeadsView: "Leads.View",
  LeadsCreate: "Leads.Create",
  LeadsEdit: "Leads.Edit",
  LeadsDelete: "Leads.Delete",
  LeadsAssign: "Leads.Assign",
  LeadsConvert: "Leads.Convert",
} as const;
