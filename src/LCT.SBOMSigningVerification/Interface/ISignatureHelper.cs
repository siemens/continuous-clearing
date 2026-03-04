// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Model;

namespace LCT.SBOMSigningVerification.Interface
{
    public interface ISignatureHelper
    {
        Signature ExtractSignature(string sbomContent);
        string RemoveSignature(string sbomContent);
    }
}
