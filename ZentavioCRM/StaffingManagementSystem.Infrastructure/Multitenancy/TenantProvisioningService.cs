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

        public TenantProvisioningService(
            PlatformDbContext platformDb,
            IOptions<TenancySettings> tenancyOptions,
            IPasswordHasher passwordHasher)
        {
            _platformDb = platformDb;
            _settings = tenancyOptions.Value;
            _passwordHasher = passwordHasher;
        }

        public async Task<IReadOnlyList<TenantDto>> GetAllAsync()
            => await _platformDb.Tenants
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(t => new TenantDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subdomain = t.Subdomain,
                    DatabaseName = t.DatabaseName,
                    Status = t.Status,
                    AdminEmail = t.AdminEmail,
                    CreatedAtUtc = t.CreatedAtUtc,
                })
                .ToListAsync();

        public async Task<ApiResponse<TenantDto>> ProvisionAsync(ProvisionTenantRequest request)
        {
            var subdomain = request.Subdomain.Trim().ToLowerInvariant();

            if (await _platformDb.Tenants.AnyAsync(t => t.Subdomain == subdomain))
            {
                return ApiResponse<TenantDto>.FailureResponse(
                    "This subdomain is already in use.",
                    ["Choose a different subdomain."]);
            }

            var databaseName = await BuildUniqueDatabaseNameAsync(subdomain);

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

                return ApiResponse<TenantDto>.SuccessResponse(
                    new TenantDto
                    {
                        Id = tenant.Id,
                        Name = tenant.Name,
                        Subdomain = tenant.Subdomain,
                        DatabaseName = tenant.DatabaseName,
                        Status = tenant.Status,
                        AdminEmail = tenant.AdminEmail,
                        CreatedAtUtc = tenant.CreatedAtUtc,
                    },
                    "Tenant provisioned.");
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
