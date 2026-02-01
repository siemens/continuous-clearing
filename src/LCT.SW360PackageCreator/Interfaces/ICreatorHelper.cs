// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Model;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// Interface for CreatorHelper
    /// </summary>
    public interface ICreatorHelper
    {
        /// <summary>
        /// Asynchronously sets contents for comparison BOM from the list of components.
        /// </summary>
        /// <param name="lstComponentForBOM">The list of components for BOM.</param>
        /// <param name="sw360Service">The SW360 service instance.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of comparison BOM data.</returns>
        public Task<List<ComparisonBomData>> SetContentsForComparisonBOM(List<Components> lstComponentForBOM, ISW360Service sw360Service);
        
        /// <summary>
        /// Asynchronously downloads release attachment source for a component.
        /// </summary>
        /// <param name="component">The comparison BOM data component.</param>
        /// <returns>A task representing the asynchronous operation that returns a dictionary of attachment URLs.</returns>
        public Task<Dictionary<string, string>> DownloadReleaseAttachmentSource(ComparisonBomData component);
        
        /// <summary>
        /// Gets creator KPI data from the updated comparison BOM data.
        /// </summary>
        /// <param name="updatedCompareBomData">The updated comparison BOM data list.</param>
        /// <returns>The creator KPI data.</returns>
        public CreatorKpiData GetCreatorKpiData(List<ComparisonBomData> updatedCompareBomData);
        
        /// <summary>
        /// Writes creator KPI data to the console.
        /// </summary>
        /// <param name="creatorKpiData">The creator KPI data to write.</param>
        public void WriteCreatorKpiDataToConsole(CreatorKpiData creatorKpiData);
        
        /// <summary>
        /// Writes source not found list to the console.
        /// </summary>
        /// <param name="comparisionBomDataList">The comparison BOM data list.</param>
        /// <param name="appSetting">The common application settings.</param>
        public void WriteSourceNotFoundListToConsole(List<ComparisonBomData> comparisionBomDataList, CommonAppSettings appSetting);
        
        /// <summary>
        /// Gets the list of components with download URL not found.
        /// </summary>
        /// <param name="comparisionBomDataList">The comparison BOM data list.</param>
        /// <returns>A list of comparison BOM data with missing download URLs.</returns>
        public List<ComparisonBomData> GetDownloadUrlNotFoundList(List<ComparisonBomData> comparisionBomDataList);
        
        /// <summary>
        /// Asynchronously gets updated component details and merges with the BOM.
        /// </summary>
        /// <param name="ListofBomComponents">The list of BOM components.</param>
        /// <param name="updatedCompareBomData">The updated comparison BOM data list.</param>
        /// <param name="sw360Service">The SW360 service instance.</param>
        /// <param name="bom">The BOM to update.</param>
        /// <returns>A task representing the asynchronous operation that returns the updated BOM.</returns>
        public Task<Bom> GetUpdatedComponentsDetails(List<Components> ListofBomComponents, List<ComparisonBomData> updatedCompareBomData, ISW360Service sw360Service, Bom bom);
    }
}
