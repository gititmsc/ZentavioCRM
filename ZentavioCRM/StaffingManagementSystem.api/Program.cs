using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ZentavioCRM.Api.Middleware;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
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
    });
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
    });

// One policy per permission code — e.g. [Authorize(Policy = PermissionCodes.LeadsCreate)] maps
// straight to a claim check against the "permission" claims the JWT carries for the user's role.
builder.Services.AddAuthorization(options =>
{
    foreach (var code in PermissionCodes.All)
    {
        options.AddPolicy(code, policy => policy.RequireClaim(PermissionCodes.ClaimType, code));
    }
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
app.UseMiddleware<TenantResolutionMiddleware>();   // resolves which tenant DB this request targets
app.UseAuthentication();   // must run before UseAuthorization so HttpContext.User is populated
app.UseAuthorization();
app.MapControllers();

app.Run();
