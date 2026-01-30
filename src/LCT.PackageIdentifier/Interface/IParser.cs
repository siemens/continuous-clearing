// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// IParser interface
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Parses the package file and generates a BOM.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="unSupportedBomList">The BOM list for unsupported components.</param>
        /// <returns>The parsed BOM.</returns>
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList);
        
        /// <summary>
        /// Asynchronously identifies internal components from the component data.
        /// </summary>
        /// <param name="componentData">The component identification data.</param>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <param name="bomhelper">The BOM helper instance.</param>
        /// <returns>A task representing the asynchronous operation that returns the updated component identification data.</returns>
        public Task<ComponentIdentification> IdentificationOfInternalComponents(
            ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper);
        
        /// <summary>
        /// Asynchronously gets JFrog repository details for the specified components.
        /// </summary>
        /// <param name="componentsForBOM">The list of components to get repository details for.</param>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <param name="bomhelper">The BOM helper instance.</param>
        /// <returns>A task representing the asynchronous operation that returns the list of components with repository details.</returns>
        public Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper);
    }
}
