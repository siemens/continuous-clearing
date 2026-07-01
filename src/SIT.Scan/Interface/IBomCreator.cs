// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications.Model;
using SIT.Common;
using SIT.Common.Interface;
using SIT.Common.Model;
using SIT.Services.Interface;
using System.Threading.Tasks;

namespace SIT.Scan.Interface
{
    /// <summary>
    /// BomCreator interface
    /// </summary>
    public interface IBomCreator
    {
        /// <summary>
        /// Gets or sets the JFrog service instance.
        /// </summary>
        public IJFrogService JFrogService { get; set; }

        /// <summary>
        /// Gets or sets the BOM helper instance.
        /// </summary>
        public IBomHelper BomHelper { get; set; }

        /// <summary>
        /// Asynchronously generates a Bill of Materials (BOM) for the specified project.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="bomHelper">The BOM helper instance.</param>
        /// <param name="fileOperations">The file operations instance.</param>
        /// <param name="projectReleases">The project releases information.</param>
        /// <param name="caToolInformation">The CA tool information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task GenerateBom(CommonAppSettings appSettings, IBomHelper bomHelper, IFileOperations fileOperations,
                                ProjectReleases projectReleases, CatoolInfo caToolInformation);

        /// <summary>
        /// Asynchronously checks the connection to JFrog.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <returns>A task representing the asynchronous operation that returns true if the connection is successful; otherwise, false.</returns>
        public Task<bool> CheckJFrogConnection(CommonAppSettings appSettings);
    }
}
