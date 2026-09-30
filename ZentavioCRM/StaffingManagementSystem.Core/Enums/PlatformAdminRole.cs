namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Role of a <see cref="Entities.Platform.PlatformAdmin"/> within the Super Admin panel.
    /// <see cref="SuperAdmin"/> can do everything (provision/suspend/reactivate/stop tenants,
    /// manage billing, manage other platform admins, run quick admin actions). <see cref="Support"/>
    /// is read-only — can view every tenant, audit log entry and dashboard, but every mutating
    /// action is rejected by the <c>PlatformSuperAdmin</c> authorization policy. Every platform
    /// admin created before this role existed is treated as <see cref="SuperAdmin"/> (see the
    /// guarded SQL default), so introducing roles never silently locks anyone out.
    /// </summary>
    public enum PlatformAdminRole
    {
        SuperAdmin = 1,
        Support = 2,
    }
}
