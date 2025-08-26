// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using ArtifactoryUploader;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Logging;
using LCT.Common.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Level = log4net.Core.Level;

namespace LCT.ArtifactoryUploader
{
    /// <summary>
    /// PackageUploader class 
    /// </summary>
    public static class PackageUploader
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly UploaderKpiData uploaderKpiData = new UploaderKpiData();
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();

        public static async Task UploadPackageToArtifactory(CommonAppSettings appSettings)
        {
            //Reading the CycloneBOM data
            var bomFilePath = Path.Combine(appSettings.Directory.OutputFolder, appSettings.SW360.ProjectName + "_" + FileConstant.BomFileName);
            Bom m_ComponentsInBOM = PackageUploadHelper.GetComponentListFromComparisonBOM(bomFilePath, environmentHelper);

            LoggerHelper.DisplayAllSettings(m_ComponentsInBOM.Components, appSettings);
            uploaderKpiData.ComponentInComparisonBOM = m_ComponentsInBOM.Components.Count;

            DisplayPackagesInfo displayPackagesInfo = PackageUploadInformation.GetComponentsToBePackages();

            List<ComponentsToArtifactory> m_ComponentsToBeUploaded = await UploadToArtifactory.GetComponentsToBeUploadedToArtifactory(m_ComponentsInBOM.Components, appSettings, displayPackagesInfo);
            //Uploading the component to artifactory

            uploaderKpiData.PackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.ClearedThirdParty);
            uploaderKpiData.DevPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Development);
            uploaderKpiData.InternalPackagesToBeUploaded = m_ComponentsToBeUploaded.Count(x => x.PackageType == PackageType.Internal);

            await PackageUploadHelper.UploadingThePackages(m_ComponentsToBeUploaded, appSettings.TimeOut, displayPackagesInfo);

            //Display packages information 
            PackageUploadInformation.DisplayPackageUploadInformation(displayPackagesInfo);


            //Updating the component's new location
            var fileOperations = new FileOperations();
            string bomGenerationPath = Path.GetDirectoryName(bomFilePath);
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref m_ComponentsInBOM, m_ComponentsToBeUploaded);

            //update Jfrog Repository Path For Successfully Uploaded Items
            m_ComponentsInBOM = await JfrogRepoUpdater.UpdateJfrogRepoPathForSucessfullyUploadedItems(m_ComponentsInBOM, displayPackagesInfo);

            var formattedString = CycloneDX.Json.Serializer.Serialize(m_ComponentsInBOM);

            // write final out put in the JSON file
            fileOperations.WriteContentToOutputBomFile(formattedString, bomGenerationPath,
                FileConstant.BomFileName, appSettings.SW360.ProjectName);

            // write KPI info to console table 
            if (Program.UploaderStopWatch != null)
                uploaderKpiData.TimeTakenByArtifactoryUploader =
                TimeSpan.FromMilliseconds(Program.UploaderStopWatch.ElapsedMilliseconds).TotalSeconds;
            PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData);

            Logger.Debug($"UploadPackageToArtifactory():End");

            // set the error code
            if (uploaderKpiData.PackagesNotUploadedDueToError > 0 || uploaderKpiData.PackagesNotExistingInRemoteCache > 0)
            {
                environmentHelper.CallEnvironmentExit(2);
                Logger.Debug("Setting ExitCode to 2");
            }
        }        
    }
}
