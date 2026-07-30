/**
 * Mirrors ZentavioCRM.Core.Common.PermissionCodes on the backend. Kept as plain string
 * constants (not an enum) so they serialize identically to the "permission" claims in the JWT.
 */
export const PermissionCodes = {
  DepartmentsView: "Departments.View",
  DepartmentsManage: "Departments.Manage",

  TerritoriesView: "Territories.View",
  TerritoriesManage: "Territories.Manage",

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

  OpportunitiesView: "Opportunities.View",
  OpportunitiesCreate: "Opportunities.Create",
  OpportunitiesEdit: "Opportunities.Edit",
  OpportunitiesDelete: "Opportunities.Delete",
  OpportunitiesAssign: "Opportunities.Assign",

  QuotationsView: "Quotations.View",
  QuotationsCreate: "Quotations.Create",
  QuotationsEdit: "Quotations.Edit",
  QuotationsDelete: "Quotations.Delete",
  QuotationsAssign: "Quotations.Assign",

  SalesOrdersView: "SalesOrders.View",
  SalesOrdersCreate: "SalesOrders.Create",
  SalesOrdersEdit: "SalesOrders.Edit",
  SalesOrdersAssign: "SalesOrders.Assign",
} as const;
