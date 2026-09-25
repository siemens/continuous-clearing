// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace SIT.SBOMSigningVerification.Interface
{
    public interface IJsonFileHelper
    {
        string SignSBOMFile();
        public void ReadSBOMFile(string sbomFilePath, out bool isValid);

        /// <summary>
        /// Reads and verifies the SBOM file in a single disk read, returning the exact
        /// content whose signature was verified. Forwarding this verified content to the
        /// consumer avoids a second, independent read from disk (TOCTOU protection).
        /// </summary>
        public void ReadSBOMFile(string sbomFilePath, out bool isValid, out string verifiedContent);
    }
}
