using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Infrastructure.Security
{
    /// <summary>
    /// Issues signed JWT access tokens using the configured <see cref="JwtSettings"/>.
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings;

        public JwtTokenGenerator(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
            };

            // One claim per granted permission so Api policies can do a single RequireClaim check.
            var permissionCodes = user.Role?.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.Code)
                ?? [];

            claims.AddRange(permissionCodes.Select(code => new Claim(PermissionCodes.ClaimType, code)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
        }
    }
}
