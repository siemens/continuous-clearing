// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public string GetFullNameOfComponent(Component item);
        public Task<List<AqlResult>> GetListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService);
    }
}
