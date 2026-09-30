using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="ITenantProvisioningService"/>
    public class TenantProvisioningService : ITenantProvisioningService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly TenancySettings _settings;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPlatformAuditLogService _auditLog;

        public TenantProvisioningService(
            PlatformDbContext platformDb,
            IOptions<TenancySettings> tenancyOptions,
            IPasswordHasher passwordHasher,
            IPlatformAuditLogService auditLog)
        {
            _platformDb = platformDb;
            _settings = tenancyOptions.Value;
            _passwordHasher = passwordHasher;
            _auditLog = auditLog;
        }

        public async Task<IReadOnlyList<TenantDto>> GetAllAsync()
            => await _platformDb.Tenants
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(t => Map(t))
                .ToListAsync();

        public async Task<ApiResponse<TenantDto>> GetByIdAsync(Guid id)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            return tenant is null
                ? ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."])
                : ApiResponse<TenantDto>.SuccessResponse(Map(tenant));
        }

        public async Task<ApiResponse<TenantDto>> SuspendAsync(Guid id, string? reason, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            if (tenant.Status != TenantStatus.Active)
            {
                return ApiResponse<TenantDto>.FailureResponse(
                    $"This tenant is {tenant.Status} and cannot be suspended from here.",
                    [$"Only an Active tenant can be suspended."]);
            }

            tenant.Status = TenantStatus.Suspended;
            await _platformDb.SaveChangesAsync();

            var summary = string.IsNullOrWhiteSpace(reason)
                ? $"Suspended tenant \"{tenant.Name}\"."
                : $"Suspended tenant \"{tenant.Name}\". Reason: {reason.Trim()}";
            await _auditLog.LogAsync(performedByAdminId, "TenantSuspended", summary, tenant.Id);

            return ApiResponse<TenantDto>.SuccessResponse(Map(tenant), "Tenant suspended.");
        }

        public async Task<ApiResponse<TenantDto>> ReactivateAsync(Guid id, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            if (tenant.Status is not (TenantStatus.Suspended or TenantStatus.Terminated))
            {
                return ApiResponse<TenantDto>.FailureResponse(
                    $"This tenant is {tenant.Status} and cannot be reactivated from here.",
                    ["Only a Suspended or Terminated tenant can be reactivated."]);
            }

            var previousStatus = tenant.Status;
            tenant.Status = TenantStatus.Active;
            await _platformDb.SaveChangesAsync();

            await _auditLog.LogAsync(
                performedByAdminId,
                "TenantReactivated",
                $"Reactivated tenant \"{tenant.Name}\" (was {previousStatus}).",
                tenant.Id);

            return ApiResponse<TenantDto>.SuccessResponse(Map(tenant), "Tenant reactivated.");
        }

        public async Task<ApiResponse<TenantDto>> StopAsync(Guid id, string? reason, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            if (tenant.Status is not (TenantStatus.Active or TenantStatus.Suspended))
            {
                return ApiResponse<TenantDto>.FailureResponse(
                    $"This tenant is {tenant.Status} and cannot be stopped from here.",
                    ["Only an Active or Suspended tenant can be stopped."]);
            }

            tenant.Status = TenantStatus.Terminated;
            await _platformDb.SaveChangesAsync();

            var summary = string.IsNullOrWhiteSpace(reason)
                ? $"Stopped tenant \"{tenant.Name}\"."
                : $"Stopped tenant \"{tenant.Name}\". Reason: {reason.Trim()}";
            await _auditLog.LogAsync(performedByAdminId, "TenantStopped", summary, tenant.Id);

            return ApiResponse<TenantDto>.SuccessResponse(Map(tenant), "Tenant stopped. This is reversible — reactivate it to restore access.");
        }

        private static TenantDto Map(Tenant t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Subdomain = t.Subdomain,
            DatabaseName = t.DatabaseName,
            Status = t.Status,
            AdminEmail = t.AdminEmail,
            CreatedAtUtc = t.CreatedAtUtc,
            ActivatedAtUtc = t.ActivatedAtUtc,
            PlanTier = t.PlanTier,
            MaxUsers = t.MaxUsers,
            MaxStorageMB = t.MaxStorageMB,
            MaxRecords = t.MaxRecords,
            PaymentStatus = t.PaymentStatus,
            BillingAmount = t.BillingAmount,
            BillingCurrency = t.BillingCurrency,
            BillingCycle = t.BillingCycle,
            NextDueDateUtc = t.NextDueDateUtc,
        };

        public async Task<ApiResponse<TenantDto>> UpdateMetadataAsync(Guid id, UpdateTenantMetadataRequest request, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var previousName = tenant.Name;
            var previousAdminEmail = tenant.AdminEmail;

            tenant.Name = request.Name.Trim();
            tenant.AdminEmail = request.AdminEmail.Trim().ToLowerInvariant();

            await _platformDb.SaveChangesAsync();

            var changes = new List<string>();
            if (previousName != tenant.Name)
            {
                changes.Add($"name \"{previousName}\" -> \"{tenant.Name}\"");
            }
            if (previousAdminEmail != tenant.AdminEmail)
            {
                changes.Add($"directory admin email \"{previousAdminEmail}\" -> \"{tenant.AdminEmail}\"");
            }

            var summary = changes.Count == 0
                ? $"Updated tenant \"{tenant.Name}\"'s metadata (no field changes)."
                : $"Updated tenant \"{tenant.Name}\"'s metadata: {string.Join(", ", changes)}.";
            await _auditLog.LogAsync(performedByAdminId, "TenantMetadataUpdated", summary, tenant.Id);

            return ApiResponse<TenantDto>.SuccessResponse(Map(tenant), "Tenant metadata updated.");
        }

        public async Task<ApiResponse<TenantDto>> UpdatePlanAsync(Guid id, UpdateTenantPlanRequest request, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var defaults = PlanTierDefaults.For(request.PlanTier);
            var previousTier = tenant.PlanTier;

            tenant.PlanTier = request.PlanTier;
            tenant.MaxUsers = request.MaxUsers ?? defaults.MaxUsers;
            tenant.MaxStorageMB = request.MaxStorageMB ?? defaults.MaxStorageMB;
            tenant.MaxRecords = request.MaxRecords ?? defaults.MaxRecords;

            await _platformDb.SaveChangesAsync();

            var summary = previousTier == tenant.PlanTier
                ? $"Updated \"{tenant.Name}\"'s plan limits (still {tenant.PlanTier})."
                : $"Changed \"{tenant.Name}\"'s plan from {previousTier} to {tenant.PlanTier}.";
            await _auditLog.LogAsync(performedByAdminId, "TenantPlanChanged", summary, tenant.Id);

            return ApiResponse<TenantDto>.SuccessResponse(Map(tenant), "Plan updated.");
        }

        public async Task<ApiResponse<TenantDto>> ProvisionAsync(ProvisionTenantRequest request, Guid? performedByAdminId)
        {
            var subdomain = request.Subdomain.Trim().ToLowerInvariant();

            if (await _platformDb.Tenants.AnyAsync(t => t.Subdomain == subdomain))
            {
                return ApiResponse<TenantDto>.FailureResponse(
                    "This subdomain is already in use.",
                    ["Choose a different subdomain."]);
            }

            var databaseName = await BuildUniqueDatabaseNameAsync(subdomain);
            var planTier = request.PlanTier ?? PlanTier.Trial;
            var limits = PlanTierDefaults.For(planTier);

            // Reserve the subdomain + database name immediately (Status = Provisioning) so a
            // concurrent request can't grab the same one while we're still creating the database.
            var tenant = new Tenant
            {
                Name = request.CompanyName.Trim(),
                Subdomain = subdomain,
                DatabaseName = databaseName,
                Status = TenantStatus.Provisioning,
                AdminEmail = request.AdminEmail.Trim().ToLowerInvariant(),
                CreatedAtUtc = DateTime.UtcNow,
                PlanTier = planTier,
                MaxUsers = limits.MaxUsers,
                MaxStorageMB = limits.MaxStorageMB,
                MaxRecords = limits.MaxRecords,
            };
            _platformDb.Tenants.Add(tenant);
            await _platformDb.SaveChangesAsync();

            try
            {
                await CreateDatabaseAsync(databaseName);

                var tenantConnectionString = BuildTenantConnectionString(databaseName);
                await ApplySchemaAndRbacSeedAsync(tenantConnectionString);
                await SeedCompanyAndAdminAsync(tenantConnectionString, request);

                tenant.Status = TenantStatus.Active;
                tenant.ActivatedAtUtc = DateTime.UtcNow;
                await _platformDb.SaveChangesAsync();

                await _auditLog.LogAsync(
                    performedByAdminId,
                    "TenantProvisioned",
                    $"Provisioned tenant \"{tenant.Name}\" ({tenant.Subdomain}).",
                    tenant.Id);

                return ApiResponse<TenantDto>.SuccessResponse(Map(tenant), "Tenant provisioned.");
            }
            catch (Exception ex)
            {
                // Best-effort cleanup so a failed attempt doesn't leave an orphaned database or
                // permanently block the subdomain — neither failure here should mask the original error.
                try
                {
                    await DropDatabaseIfExistsAsync(databaseName);
                }
                catch
                {
                    // ignored — original exception is what gets reported below.
                }

                tenant.Status = TenantStatus.Failed;
                try
                {
                    await _platformDb.SaveChangesAsync();
                }
                catch
                {
                    // ignored — same reasoning.
                }

                return ApiResponse<TenantDto>.FailureResponse("Tenant provisioning failed.", [ex.Message]);
            }
        }

        private async Task<string> BuildUniqueDatabaseNameAsync(string subdomain)
        {
            var sanitized = new string(subdomain.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var candidate = $"{_settings.TenantDatabaseNamePrefix}{sanitized}";

            if (!await _platformDb.Tenants.AnyAsync(t => t.DatabaseName == candidate))
            {
                return candidate;
            }

            // Extremely unlikely (would require two different subdomains sanitizing to the same
            // string), but cheap to guard against rather than fail provisioning outright.
            return $"{candidate}_{Guid.NewGuid():N}"[..Math.Min(128, candidate.Length + 33)];
        }

        private string BuildMasterConnectionString() => $"{_settings.SqlServerHostConnectionString};Database=master;";

        private string BuildTenantConnectionString(string databaseName) => $"{_settings.SqlServerHostConnectionString};Database={databaseName};";

        private async Task CreateDatabaseAsync(string databaseName)
        {
            await using var connection = new SqlConnection(BuildMasterConnectionString());
            await connection.OpenAsync();

            await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync();
        }

        private async Task DropDatabaseIfExistsAsync(string databaseName)
        {
            await using var connection = new SqlConnection(BuildMasterConnectionString());
            await connection.OpenAsync();

            await using var command = new SqlCommand(
                $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{databaseName}]; END",
                connection)
            { CommandTimeout = 60 };
            await command.ExecuteNonQueryAsync();
        }

        private static async Task ApplySchemaAndRbacSeedAsync(string tenantConnectionString)
        {
            await using var connection = new SqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            await ExecuteBatchesAsync(connection, ReadEmbeddedSql("TenantSchema.sql"));
            await ExecuteBatchesAsync(connection, ReadEmbeddedSql("TenantRbacSeed.sql"));
        }

        private async Task SeedCompanyAndAdminAsync(string tenantConnectionString, ProvisionTenantRequest request)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(tenantConnectionString).Options;
            await using var tenantDb = new AppDbContext(options);

            var company = new Company
            {
                Name = request.CompanyName.Trim(),
                DefaultCurrency = "USD",
                TimeZone = "UTC",
                CreatedAtUtc = DateTime.UtcNow,
            };
            tenantDb.Companies.Add(company);
            await tenantDb.SaveChangesAsync();

            var department = new Department
            {
                CompanyId = company.Id,
                Name = "Sales",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            tenantDb.Departments.Add(department);
            await tenantDb.SaveChangesAsync();

            var adminUser = new User
            {
                EmployeeCode = "EMP-0001",
                FirstName = request.AdminFirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(request.AdminLastName) ? string.Empty : request.AdminLastName.Trim(),
                Email = request.AdminEmail.Trim().ToLowerInvariant(),
                PasswordHash = _passwordHasher.Hash(request.AdminPassword),
                RoleId = SeedIds.AdminRoleId,
                DepartmentId = department.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            tenantDb.Users.Add(adminUser);
            await tenantDb.SaveChangesAsync();
        }

        private static string ReadEmbeddedSql(string fileName)
        {
            var assembly = typeof(TenantProvisioningService).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .First(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>Splits a script on lines that are exactly "GO" — the client-side batch
        /// separator SSMS/sqlcmd understand but that raw ADO.NET commands do not.</summary>
        private static IEnumerable<string> SplitBatches(string script)
        {
            var lines = script.Replace("\r\n", "\n").Split('\n');
            var currentBatch = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentBatch.Length > 0)
                    {
                        yield return currentBatch.ToString();
                        currentBatch.Clear();
                    }
                }
                else
                {
                    currentBatch.AppendLine(line);
                }
            }

            if (currentBatch.Length > 0)
            {
                yield return currentBatch.ToString();
            }
        }

        private static async Task ExecuteBatchesAsync(SqlConnection connection, string script)
        {
            foreach (var batch in SplitBatches(script))
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 60 };
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
