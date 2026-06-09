// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.SBOMSigningVerification.Model;

namespace SIT.SBOMSigningVerification.Interface
{
    public interface ISignatureHelper
    {
        Signature? ExtractSignature(string sbomContent);
        string RemoveSignature(string sbomContent);
    }
}
