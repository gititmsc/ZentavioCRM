using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZentavioCRM.Core.Configuration;
using ZentavioCRM.Core.Entities.Platform;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Infrastructure.Security
{
    /// <summary>
    /// Issues signed JWT access tokens for Platform Admins using the configured
    /// <see cref="PlatformJwtSettings"/> — a signing key entirely separate from tenant user
    /// tokens, so the two token types can never be mistaken for one another.
    /// </summary>
    public class PlatformJwtTokenGenerator : IPlatformJwtTokenGenerator
    {
        private readonly PlatformJwtSettings _settings;

        public PlatformJwtTokenGenerator(IOptions<PlatformJwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiresAtUtc) GenerateToken(PlatformAdmin admin)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, admin.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, admin.FullName),
                new(IPlatformJwtTokenGenerator.PlatformAdminClaimType, "true"),
            };

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
