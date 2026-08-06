using Isopoh.Cryptography.Argon2;
using MoneyKeeper.Identity.Application.Common.Interfaces;

namespace MoneyKeeper.Identity.Infrastructure.Auth
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return Argon2.Hash(password);
        }

        public bool Verify(string hashedPassword, string password)
        {
            return Argon2.Verify(hashedPassword, password);
        }
    }
}
