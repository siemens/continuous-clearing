// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.PackageIdentifier.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using LCT.Common;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// IParser interface
    /// </summary>
    public interface IParser
    {
        public Bom ParsePackageFile(CommonAppSettings appSettings);
        public Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings);
        public Task<List<Component>> GetRepoDetails(List<Component> componentsForBOM, CommonAppSettings appSettings);
    }
}
