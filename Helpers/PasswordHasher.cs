using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ZerodaTrade.Helpers
{
    public static class PasswordHasher
    {
        public static (string hash, string salt) HashPassword(string password)
        {
            byte[] saltBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return (hashed, salt);
        }

        public static bool Verify(string password, string hashed, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashed) || string.IsNullOrEmpty(salt)) return false;
            byte[] saltBytes = Convert.FromBase64String(salt);
            string hashOfInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return hashOfInput == hashed;
        }
    }
}
