// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace LCT.PackageIdentifier.Model
{
    public class RuntimeInfo
    {
        #region Fields
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the full path to the project file or project folder.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the logical project name.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the project will be published as self-contained.
        /// </summary>
        public bool IsSelfContained { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SelfContained was explicitly set in the project file.
        /// </summary>
        public bool SelfContainedExplicitlySet { get; set; }

        /// <summary>
        /// Gets or sets the evaluated value for SelfContained after MSBuild evaluation.
        /// </summary>
        public string SelfContainedEvaluated { get; set; }

        /// <summary>
        /// Gets or sets the reason why SelfContained was evaluated to the given value.
        /// </summary>
        public string SelfContainedReason { get; set; }

        /// <summary>
        /// Gets or sets the list of runtime identifiers (RIDs) for the project.
        /// </summary>
        public List<string> RuntimeIdentifiers { get; set; } = new();

        /// <summary>
        /// Gets or sets framework reference information discovered in the project.
        /// </summary>
        public List<FrameworkReferenceInfo> FrameworkReferences { get; set; } = new();

        /// <summary>
        /// Gets or sets a short error message encountered while determining runtime information.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets detailed error information or stack traces for diagnostics.
        /// </summary>
        public string ErrorDetails { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }

    public class FrameworkReferenceInfo
    {
        #region Properties
        /// <summary>
        /// Gets or sets the target framework identifier (TFM) that this reference targets.
        /// </summary>
        public string TargetFramework { get; set; }

        /// <summary>
        /// Gets or sets the name of the framework reference.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the targeting pack used for this framework reference.
        /// </summary>
        public string TargetingPackVersion { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}