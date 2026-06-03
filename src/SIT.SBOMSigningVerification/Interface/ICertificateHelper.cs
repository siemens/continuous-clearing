// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


namespace SIT.SBOMSigningVerification.Interface
{
    public interface ICertificateHelper
    {
        byte[] SignCertificate(string content);
        bool VerifySignature(string content, string signature);
    }
}
