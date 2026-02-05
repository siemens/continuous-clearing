// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace LCT.Common.ComplianceValidator
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the compliance settings model containing exception components.
    /// </summary>
    public class ComplianceSettingsModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of compliance exception components.
        /// </summary>
        [JsonPropertyName("complianceExceptionComponents")]
        public List<ComplianceExceptionComponent> ComplianceExceptionComponents { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a compliance exception component with instructions.
    /// </summary>
    public class ComplianceExceptionComponent
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the component description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the list of package URLs (PURL).
        /// </summary>
        [JsonPropertyName("purl")]
        public List<string> Purl { get; set; }

        /// <summary>
        /// Gets or sets the compliance instructions.
        /// </summary>
        [JsonPropertyName("complianceInstructions")]
        public ComplianceInstructions ComplianceInstructions { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents compliance instructions with warnings and recommendations.
    /// </summary>
    public class ComplianceInstructions
    {
        #region Properties

        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        [JsonPropertyName("warningMessage")]
        public string WarningMessage { get; set; }

        /// <summary>
        /// Gets or sets the recommendation.
        /// </summary>
        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; }

        #endregion
    }
}