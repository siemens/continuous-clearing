using System.Security.Cryptography;
using System.Text;

namespace Telemetry
{
    public static class HashUtility
    {
        public static string GetHashString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            using (var hashAlgorithm = SHA256.Create())
            {
                byte[] hashBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
