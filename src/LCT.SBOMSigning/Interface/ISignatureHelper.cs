// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using SBOMSigning.Model;

namespace SBOMSigning.Interface
{
    public interface ISignatureHelper
    {
        Signature ExtractSignature(string sbomContent);
        string RemoveSignature(string sbomContent);
    }
}
