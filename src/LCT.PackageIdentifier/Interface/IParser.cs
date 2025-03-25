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
        public Bom ParsePackageFile(CommonAppSettings appSettings);
        public Task<ComponentIdentification> IdentificationOfInternalComponents(
            ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper);
        public Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper);
    }
}
