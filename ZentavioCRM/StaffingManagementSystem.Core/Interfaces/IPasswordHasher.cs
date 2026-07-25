namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Hashes and verifies user passwords. Implemented in the Infrastructure layer.
    /// </summary>
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string passwordHash);
    }
}
