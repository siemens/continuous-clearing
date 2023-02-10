// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using CycloneDX.Models;
using LCT.PackageIdentifier.Model;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// BOM helper interface
    /// </summary>
    public interface IBomHelper
    {
        public void WriteBomKpiDataToConsole(BomKpiData bomKpiData);
        public void WriteInternalComponentsListToKpi(List<Component> internalComponents);
        public string GetProjectSummaryLink(string projectId, string sw360Url);
    }
}
