// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace LCT.SBOMSigningVerification.Interface
{
    public interface ICertificateHelper
    {
        byte[] SignCertificate(string content);
        bool VerifySignature(string content, string signature);
    }
}
