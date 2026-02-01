// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// The IComponentCreator interface
    /// </summary>
    interface IComponentCreator
    {
        /// <summary>
        /// Asynchronously parses a CycloneDX BOM file.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="sw360Service">The SW360 service instance.</param>
        /// <param name="cycloneDXBomParser">The CycloneDX BOM parser instance.</param>
        /// <param name="creatorHelper">The creator helper instance.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of comparison BOM data.</returns>
        Task<List<ComparisonBomData>> CycloneDxBomParser(CommonAppSettings appSettings,
            ISW360Service sw360Service, ICycloneDXBomParser cycloneDXBomParser, ICreatorHelper creatorHelper);
        
        /// <summary>
        /// Asynchronously creates components in SW360 from the parsed BOM data.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="sw360CreatorService">The SW360 creator service instance.</param>
        /// <param name="sw360Service">The SW360 service instance.</param>
        /// <param name="sw360ProjectService">The SW360 project service instance.</param>
        /// <param name="fileOperations">The file operations instance.</param>
        /// <param name="creatorHelper">The creator helper instance.</param>
        /// <param name="parsedBomData">The parsed BOM data list.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateComponentInSw360(CommonAppSettings appSettings,
            ISw360CreatorService sw360CreatorService, ISW360Service sw360Service, ISw360ProjectService sw360ProjectService,
            IFileOperations fileOperations, ICreatorHelper creatorHelper, List<ComparisonBomData> parsedBomData);
    }
}
