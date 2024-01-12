// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.PackageIdentifier.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using LCT.Common;
using LCT.Services.Interface;

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
