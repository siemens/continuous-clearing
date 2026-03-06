// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace LCT.SBOMSigningVerification.Interface
{
    public interface IJsonFileHelper
    {
        string SignSBOMFile();
        public void ReadSBOMFile(string sbomFilePath, out bool isValid);
    }
}
