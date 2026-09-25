using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ZentavioCRM.Api.Authorization;
using ZentavioCRM.Api.Json;
using ZentavioCRM.Api.Middleware;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Interfaces;
using ZentavioCRM.Infrastructure.Extensions;
using ZentavioCRM.Repositories.Extensions;
using ZentavioCRM.Services.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Every enum in Core.Enums (LeadStatus, LeadSource, CustomerType, ActivityType, ...) is
        // stored as its string name in the database (HasConversion<string>()) and the React
        // frontend's types are string-literal unions ("New", "Qualified", ...) that match those
        // names exactly. Without this converter System.Text.Json defaults to serializing enums as
        // their numeric value, which silently breaks every status/type comparison and lookup on
        // the frontend (e.g. NEXT_STATUSES[lead.status] in LeadDetail.tsx would receive a number
        // instead of "Qualified" and resolve to undefined).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // react-hook-form's register() returns raw string values from <input type="number">
        // unless every single call site opts in with { valueAsNumber: true } — several forms in
        // this codebase don't (LeadForm's Budget/ExpectedValue, CustomerForm's AnnualRevenue/
        // CreditLimit, etc.), so numeric fields routinely arrive as JSON strings (e.g. "50000")
        // rather than JSON numbers. System.Text.Json rejects that for decimal/int targets by
        // default. Allowing string-to-number coercion here fixes every current and future form
        // at once instead of chasing down each input individually.
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        // Same story as above, but for Guid?/DateTime? — AllowReadingFromString's "" -> null
        // leniency only covers the built-in *numeric* converters, not Guid or DateTime. A
        // left-at-"Unassigned"/"None" <select> (AssignedToUserId, TerritoryId, LinkedCustomerId)
        // or an empty <input type="date"> (NextFollowUpDate, ExpectedCloseDate, ...) submits ""
        // for those too, which the stock converters reject — surfacing as a generic, no-field-
        // highlighted "One or more validation errors occurred" with no indication of which field.
        // See Json/EmptyStringAsNullConverters.cs for the full explanation.
        options.JsonSerializerOptions.Converters.Add(new EmptyStringAsNullGuidConverter());
        options.JsonSerializerOptions.Converters.Add(new EmptyStringAsNullDateTimeConverter());
    });

// [ApiController]'s automatic 400 response normally echoes ModelState error messages verbatim —
// fine for our own DataAnnotations messages (e.g. "Company name is required."), but System.Text.
// Json's own deserialization-failure messages are raw exception text meant for a developer (e.g.
// "The JSON value could not be converted to System.Nullable`1[System.Guid]. Path: $.territoryId |
// LineNumber: 0 | BytePositionInLine: 414."). Swap those specific technical messages for a plain
// one instead — the frontend already highlights the exact offending field red (see LeadForm.tsx's
// applyServerFieldErrors), so the message itself doesn't need to spell out which field or why.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var friendlyState = new ModelStateDictionary();
        foreach (var (key, entry) in context.ModelState)
        {
            foreach (var error in entry.Errors)
            {
                var message = LooksLikeRawJsonExceptionMessage(error.ErrorMessage)
                    ? "This value isn't valid — please check this field and try again."
                    : error.ErrorMessage;
                friendlyState.AddModelError(key, message);
            }
        }

        var problemDetails = new ValidationProblemDetails(friendlyState)
        {
            Title = "Please correct the highlighted field(s) and try again.",
            Status = StatusCodes.Status400BadRequest,
        };

        return new BadRequestObjectResult(problemDetails);
    };
});

static bool LooksLikeRawJsonExceptionMessage(string message) =>
    message.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase) ||
    message.Contains("System.Text.Json", StringComparison.OrdinalIgnoreCase) ||
    message.Contains("BytePositionInLine", StringComparison.OrdinalIgnoreCase) ||
    message.Contains("could not be converted to System.", StringComparison.OrdinalIgnoreCase);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ZentavioCRM API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token returned by /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };

    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

// Layered service registration — Api composes Infrastructure, Repositories and Services.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddBusinessServices();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var platformJwtSettings = builder.Configuration.GetSection(PlatformJwtSettings.SectionName).Get<PlatformJwtSettings>() ?? new PlatformJwtSettings();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    })
    // Second, independently-signed scheme for Platform Admin sessions (Super Admin panel) — a
    // distinct secret/issuer/audience from the tenant-user scheme above means a token minted for
    // one can never validate against the other, regardless of policy configuration. Only
    // Controllers/Platform/* opt into this scheme via [Authorize(Policy = PlatformAuthorizationPolicies.PlatformAdmin)].
    .AddJwtBearer(PlatformAuthorizationPolicies.PlatformAdmin, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = platformJwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = platformJwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(platformJwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

// One policy per permission code — e.g. [Authorize(Policy = PermissionCodes.LeadsCreate)] maps
// straight to a claim check against the "permission" claims the JWT carries for the user's role.
builder.Services.AddAuthorization(options =>
{
    foreach (var code in PermissionCodes.All)
    {
        options.AddPolicy(code, policy => policy.RequireClaim(PermissionCodes.ClaimType, code));
    }

    // Scoped to the "PlatformAdmin" scheme only — a request bearing a normal tenant-user JWT
    // (even one with every permission claim) is authenticated under the default scheme, not this
    // one, so it's rejected before the claim check even runs.
    options.AddPolicy(PlatformAuthorizationPolicies.PlatformAdmin, policy => policy
        .AddAuthenticationSchemes(PlatformAuthorizationPolicies.PlatformAdmin)
        .RequireClaim(IPlatformJwtTokenGenerator.PlatformAdminClaimType, "true"));
});

// CORS — allow the Vite dev server
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZentavioCRM API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseMiddleware<ExceptionHandlingMiddleware>();  // keeps CORS headers on error responses instead of a bare, header-less 500
app.UseMiddleware<TenantResolutionMiddleware>();   // resolves which tenant DB this request targets
app.UseAuthentication();   // must run before UseAuthorization so HttpContext.User is populated
app.UseAuthorization();
app.MapControllers();

app.Run();
