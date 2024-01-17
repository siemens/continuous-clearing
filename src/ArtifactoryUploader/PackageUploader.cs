// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
using System.Linq;
using LCT.Common.Constants;
using System.IO;

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

            uploaderKpiData.PackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.ClearedThirdParty);
            uploaderKpiData.DevPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Development);
            uploaderKpiData.InternalPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Internal);

            await PackageUploadHelper.UploadingThePackages(m_ComponentsToBeUploaded, appSettings.TimeOut);

            //Updating the component's new location
            var fileOperations = new FileOperations();
            string bomGenerationPath = Path.GetDirectoryName(appSettings.BomFilePath);
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref m_ComponentsInBOM, m_ComponentsToBeUploaded);
            fileOperations.WriteContentToFile(m_ComponentsInBOM, bomGenerationPath, FileConstant.BomFileName, appSettings.SW360ProjectName);

            // write kpi info to console table 
            PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData);
            if (Program.UploaderStopWatch != null)
                uploaderKpiData.TimeTakenByComponentCreator =
                TimeSpan.FromMilliseconds(Program.UploaderStopWatch.ElapsedMilliseconds).TotalSeconds;
            Logger.Debug($"UploadPackageToArtifactory():End");

            // set the error code
            if(uploaderKpiData.PackagesNotUploadedDueToError > 0 || uploaderKpiData.ComponentNotApproved > 0)
            {
                Environment.ExitCode = 2;
                Logger.Debug("Setting ExitCode to 2");
            }
        }
    }
}
