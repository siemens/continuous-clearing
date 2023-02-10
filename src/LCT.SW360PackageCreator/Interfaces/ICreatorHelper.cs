// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Model;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using LCT.Common;

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// Interface for CreatorHelper
    /// </summary>
    public interface ICreatorHelper
    {
        public Task<List<ComparisonBomData>> SetContentsForComparisonBOM(List<Components> lstComponentForBOM, ISW360Service sw360Service);
        public Task<Dictionary<string, string>> DownloadReleaseAttachmentSource(ComparisonBomData component);
        public CreatorKpiData GetCreatorKpiData(List<ComparisonBomData> updatedCompareBomData);
        public void WriteCreatorKpiDataToConsole(CreatorKpiData creatorKpiData);
        public void WriteSourceNotFoundListToConsole(List<ComparisonBomData> comparisionBomDataList, CommonAppSettings appSetting);
        public List<ComparisonBomData> GetDownloadUrlNotFoundList(List<ComparisonBomData> comparisionBomDataList);
        public Task<Bom> GetUpdatedComponentsDetails(List<Components> ListofBomComponents, List<ComparisonBomData> updatedCompareBomData, ISW360Service sw360Service, Bom bom);
    }
}
