// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LCT.Common;

namespace LCT.ArtifactoryUploader
{
    /// <summary>
    /// PackageUploader class 
    /// </summary>
    public static class PackageUploader
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly UploaderKpiData uploaderKpiData = new UploaderKpiData();

        public static async Task UploadPackageToArtifactory(CommonAppSettings appSettings)
        {
            //Reading the CycloneBOM data

            Bom m_ComponentsInBOM = PackageUploadHelper.GetComponentListFromComparisonBOM(appSettings.BomFilePath);

            uploaderKpiData.ComponentInComparisonBOM = m_ComponentsInBOM.Components.Count;

            List<ComponentsToArtifactory> m_ComponentsToBeUploaded = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(m_ComponentsInBOM.Components, appSettings);
            //Uploading the component to artifactory

            uploaderKpiData.PackagesToBeUploaded = m_ComponentsToBeUploaded.Count;
            await PackageUploadHelper.UploadingThePackages(m_ComponentsToBeUploaded, appSettings.TimeOut);

            // write kpi info to console table 
            PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData);
            if (Program.UploaderStopWatch != null)
                uploaderKpiData.TimeTakenByComponentCreator =
                TimeSpan.FromMilliseconds(Program.UploaderStopWatch.ElapsedMilliseconds).TotalSeconds;
            Logger.Debug($"UploadPackageToArtifactory():End");
        }
    }
}
