// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace LCT.Common.Interface
{
    /// <summary>
    /// Interface for SBOM signing and validation operations.
    /// </summary>
    public interface ISbomSigningValidation
    {
        /// <summary>
        /// Performs SBOM signing or validation based on operation type.
        /// </summary>
        /// <param name="appSettings">Application settings containing signing configuration.</param>
        /// <param name="operationType">Operation type: "sign" or "validate".</param>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        /// <param name="bomContent">Optional BOM content for signing (if not provided, reads from file).</param>
        /// <returns>
        /// For Sign operation: Returns the signed BOM content as string.
        /// For Validate operation: Returns validation result as string ("True" or "False").
        /// </returns>
        string PerformSbomOperation(CommonAppSettings appSettings, string operationType, string bomFilePath, string bomContent);

        /// <summary>
        /// Performs SBOM signing operation and returns the signed BOM content.
        /// </summary>
        /// <param name="appSettings">Application settings containing signing configuration.</param>
        /// <param name="operationType">Operation type (should be "sign").</param>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        /// <param name="bomContent">Optional BOM content for signing.</param>
        /// <returns>Signed BOM content as string.</returns>
        string PerformSbomSigning(CommonAppSettings appSettings, string operationType, string bomFilePath, string bomContent);

        /// <summary>
        /// Performs SBOM validation operation and returns the validation result.
        /// </summary>
        /// <param name="appSettings">Application settings containing signing configuration.</param>
        /// <param name="operationType">Operation type (should be "validate").</param>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        /// <returns>True if validation succeeds; otherwise false.</returns>
        bool PerformSbomSigningVerification(CommonAppSettings appSettings, string operationType, string bomFilePath);

        /// <summary>
        /// Validates SBOM signature and handles exit behavior based on IsSignVerifyRequired flag.
        /// This method encapsulates all validation logic including logging, error handling, and exit handling.
        /// </summary>
        /// <param name="appSettings">Application settings</param>
        /// <param name="bomFilePath">Path to BOM file</param>
        /// <param name="environmentHelper">Environment helper for exit handling</param>
        void SigningVerification(CommonAppSettings appSettings, string bomFilePath, IEnvironmentHelper environmentHelper);
    }
}