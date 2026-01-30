// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Security.Cryptography;
using System.Text;

namespace LCT.Telemetry
{
    /// <summary>
    /// Provides utility methods for generating hash strings.
    /// </summary>
    public static class HashUtility
    {
        #region Methods
        /// <summary>
        /// Generates a SHA256 hash string from the input string.
        /// </summary>
        /// <param name="input">The input string to hash.</param>
        /// <returns>A lowercase hexadecimal string representation of the SHA256 hash, or an empty string if the input is null or empty.</returns>
        public static string GetHashString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        #endregion
    }
}