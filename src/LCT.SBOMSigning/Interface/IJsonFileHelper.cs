// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace SBOMSigning.Interface
{
    public interface IJsonFileHelper
    {
        string SignSBOMFile();
        public void ReadSBOMFile(string sbomFilePath, out bool isValid);
    }
}
