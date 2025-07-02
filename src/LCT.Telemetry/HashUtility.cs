// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Security.Cryptography;
using System.Text;

namespace LCT.Telemetry
{
    public static class HashUtility
    {
        public static string GetHashString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}