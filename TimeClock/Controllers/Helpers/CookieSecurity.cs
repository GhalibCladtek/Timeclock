using System;
using System.Text;
using System.Web.Security; // Required for MachineKey

namespace TimeClock.Helpers
{
    public static class CookieSecurity
    {
        // A unique string to ensure this specific encryption can't be mixed up with others
        private const string Purpose = "RememberMeCookieAuth";

        public static string Encrypt(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = MachineKey.Protect(plainTextBytes, Purpose);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string encryptedText)
        {
            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var plainTextBytes = MachineKey.Unprotect(encryptedBytes, Purpose);
                return Encoding.UTF8.GetString(plainTextBytes);
            }
            catch
            {
                // If someone tampers with the cookie, decryption will fail.
                // We catch the error and return null to deny access safely.
                return null;
            }
        }
    }
}