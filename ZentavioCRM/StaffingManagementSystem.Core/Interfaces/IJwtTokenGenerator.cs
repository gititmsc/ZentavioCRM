using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Issues JWT access tokens for authenticated users. Implemented in the Infrastructure layer.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
    }
}
