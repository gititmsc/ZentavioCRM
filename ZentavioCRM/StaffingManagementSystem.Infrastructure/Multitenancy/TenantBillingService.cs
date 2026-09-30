using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Persistence;

namespace ZentavioCRM.Infrastructure.Multitenancy
{
    /// <inheritdoc cref="ITenantBillingService"/>
    public class TenantBillingService : ITenantBillingService
    {
        private readonly PlatformDbContext _platformDb;
        private readonly IPlatformAuditLogService _auditLog;
        private readonly ITenantProvisioningService _provisioningService;

        public TenantBillingService(
            PlatformDbContext platformDb,
            IPlatformAuditLogService auditLog,
            ITenantProvisioningService provisioningService)
        {
            _platformDb = platformDb;
            _auditLog = auditLog;
            _provisioningService = provisioningService;
        }

        public async Task<ApiResponse<TenantDto>> UpdateBillingAsync(Guid tenantId, UpdateTenantBillingRequest request, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return ApiResponse<TenantDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var changes = new List<string>();

            if (request.BillingAmount.HasValue && request.BillingAmount != tenant.BillingAmount)
            {
                changes.Add($"amount {tenant.BillingAmount?.ToString("0.00") ?? "(none)"} -> {request.BillingAmount:0.00}");
                tenant.BillingAmount = request.BillingAmount;
                if (string.IsNullOrWhiteSpace(tenant.BillingCurrency))
                {
                    tenant.BillingCurrency = "USD";
                }
            }

            if (!string.IsNullOrWhiteSpace(request.BillingCurrency) && request.BillingCurrency != tenant.BillingCurrency)
            {
                changes.Add($"currency {tenant.BillingCurrency ?? "(none)"} -> {request.BillingCurrency.ToUpperInvariant()}");
                tenant.BillingCurrency = request.BillingCurrency.Trim().ToUpperInvariant();
            }

            if (request.BillingCycle.HasValue && request.BillingCycle != tenant.BillingCycle)
            {
                changes.Add($"cycle {tenant.BillingCycle?.ToString() ?? "(none)"} -> {request.BillingCycle}");
                tenant.BillingCycle = request.BillingCycle;
            }

            if (request.NextDueDateUtc.HasValue && request.NextDueDateUtc != tenant.NextDueDateUtc)
            {
                changes.Add($"next due date {tenant.NextDueDateUtc?.ToString("yyyy-MM-dd") ?? "(none)"} -> {request.NextDueDateUtc:yyyy-MM-dd}");
                tenant.NextDueDateUtc = request.NextDueDateUtc;
            }

            var willAutoSuspend = request.PaymentStatus == PaymentStatus.Overdue
                && tenant.PaymentStatus != PaymentStatus.Overdue
                && tenant.Status == TenantStatus.Active;

            if (request.PaymentStatus.HasValue && request.PaymentStatus != tenant.PaymentStatus)
            {
                changes.Add($"payment status {tenant.PaymentStatus} -> {request.PaymentStatus}");
                tenant.PaymentStatus = request.PaymentStatus.Value;
            }

            await _platformDb.SaveChangesAsync();

            var summary = changes.Count == 0
                ? $"Updated \"{tenant.Name}\"'s billing details (no field changes)."
                : $"Updated \"{tenant.Name}\"'s billing details: {string.Join(", ", changes)}.";
            await _auditLog.LogAsync(performedByAdminId, "TenantBillingUpdated", summary, tenant.Id);

            // Marking a tenant Overdue automatically suspends it — same reversible lock-out as any
            // other suspension, reused rather than reimplemented. Only fires from Active, since a
            // tenant that's already Suspended/Terminated has nothing further to lock out.
            var autoSuspended = false;
            if (willAutoSuspend)
            {
                var suspendResult = await _provisioningService.SuspendAsync(
                    tenant.Id,
                    "Automatically suspended: payment marked Overdue.",
                    performedByAdminId);
                autoSuspended = suspendResult.Success;

                // SuspendAsync used its own PlatformDbContext query, so refresh our in-memory copy
                // before returning it to the caller.
                await _platformDb.Entry(tenant).ReloadAsync();
            }

            var message = autoSuspended
                ? "Billing details updated. Tenant was automatically suspended for being Overdue."
                : "Billing details updated.";
            return ApiResponse<TenantDto>.SuccessResponse(MapTenant(tenant), message);
        }

        public async Task<ApiResponse<TenantPaymentDto>> RecordPaymentAsync(Guid tenantId, RecordTenantPaymentRequest request, Guid? performedByAdminId)
        {
            var tenant = await _platformDb.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return ApiResponse<TenantPaymentDto>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var paidAt = request.PaidAtUtc ?? DateTime.UtcNow;
            var currency = string.IsNullOrWhiteSpace(request.Currency)
                ? (tenant.BillingCurrency ?? "USD")
                : request.Currency.Trim().ToUpperInvariant();

            var payment = new TenantPayment
            {
                TenantId = tenant.Id,
                Amount = request.Amount,
                Currency = currency,
                PaidAtUtc = paidAt,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                RecordedByAdminId = performedByAdminId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _platformDb.TenantPayments.Add(payment);

            tenant.PaymentStatus = PaymentStatus.Paid;
            tenant.BillingCurrency ??= currency;

            if (tenant.BillingCycle is not null)
            {
                var rollFrom = tenant.NextDueDateUtc.HasValue && tenant.NextDueDateUtc.Value > paidAt
                    ? tenant.NextDueDateUtc.Value
                    : paidAt;

                tenant.NextDueDateUtc = tenant.BillingCycle switch
                {
                    BillingCycle.Monthly => rollFrom.AddMonths(1),
                    BillingCycle.Yearly => rollFrom.AddYears(1),
                    _ => tenant.NextDueDateUtc,
                };
            }

            await _platformDb.SaveChangesAsync();

            var recordedByAdmin = performedByAdminId.HasValue
                ? await _platformDb.PlatformAdmins.FirstOrDefaultAsync(a => a.Id == performedByAdminId.Value)
                : null;

            await _auditLog.LogAsync(
                performedByAdminId,
                "PaymentRecorded",
                $"Recorded a payment of {payment.Currency} {payment.Amount:0.00} for \"{tenant.Name}\".",
                tenant.Id);

            return ApiResponse<TenantPaymentDto>.SuccessResponse(MapPayment(payment, recordedByAdmin?.Email), "Payment recorded.");
        }

        public async Task<ApiResponse<IReadOnlyList<TenantPaymentDto>>> GetPaymentsAsync(Guid tenantId)
        {
            if (!await _platformDb.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                return ApiResponse<IReadOnlyList<TenantPaymentDto>>.FailureResponse("Tenant not found.", ["Tenant not found."]);
            }

            var payments = await _platformDb.TenantPayments
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.PaidAtUtc)
                .Select(p => new
                {
                    Payment = p,
                    RecordedByAdminEmail = p.RecordedByAdmin != null ? p.RecordedByAdmin.Email : null,
                })
                .ToListAsync();

            var dtos = payments.Select(x => MapPayment(x.Payment, x.RecordedByAdminEmail)).ToList();
            return ApiResponse<IReadOnlyList<TenantPaymentDto>>.SuccessResponse(dtos);
        }

        private static TenantPaymentDto MapPayment(TenantPayment p, string? recordedByAdminEmail) => new()
        {
            Id = p.Id,
            TenantId = p.TenantId,
            Amount = p.Amount,
            Currency = p.Currency,
            PaidAtUtc = p.PaidAtUtc,
            Note = p.Note,
            RecordedByAdminEmail = recordedByAdminEmail ?? "System",
            CreatedAtUtc = p.CreatedAtUtc,
        };

        private static TenantDto MapTenant(Tenant t) => new()
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
    }
}
