using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ZentavioCRM.Api.Extensions
{
    /// <summary>Small helpers for reading the current user's identity out of the JWT claims.</summary>
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
