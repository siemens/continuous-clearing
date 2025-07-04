﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Uploader Artifactory

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Facade;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using log4net;
using log4net.Core;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ArtifactoryUploader
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static bool m_Verbose = false;
        public static Stopwatch UploaderStopWatch { get; set; }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        static async Task Main(string[] args)
        {
            UploaderStopWatch = new Stopwatch();
            UploaderStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;

            ISettingsManager settingsManager = new SettingsManager();
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();
            Log4Net.CatoolCurrentDirectory = System.IO.Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;
            CommonHelper.DefaultLogFolderInitialisation(FileConstant.ArtifactoryUploaderLog, m_Verbose);
            CommonAppSettings appSettings = settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName);

            string FolderPath = CommonHelper.LogFolderInitialisation(appSettings, FileConstant.ArtifactoryUploaderLog, m_Verbose);
            Logger.Logger.Log(null, Level.Debug, $"log manager initiated folder path: {FolderPath}", null);
            settingsManager.CheckRequiredArgsToRun(appSettings, "Uploader");

            Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Artifactory Uploader >>>>>====================", null);
            Logger.Logger.Log(null, Level.Notice, $"\nStart of Artifactory Uploader execution: {DateTime.Now}", null);

            if (!appSettings.Jfrog.DryRun)
                Logger.Logger.Log(null, Level.Alert, $"Artifactory Uploader is running in release mode !!! \n", null);
            else
                Logger.Logger.Log(null, Level.Alert, $"Artifactory Uploader is running in dry-run mode, no packages will be moved \n", null);

            var bomFilePath = Path.Combine(appSettings.Directory.OutputFolder, appSettings.SW360.ProjectName + "_" + FileConstant.BomFileName);

            Logger.Logger.Log(null, Level.Info, $"Input Parameters used in Artifactory Uploader:\n\t", null);
            Logger.Logger.Log(null, Level.Notice, $"\tBomFilePath:\t\t {bomFilePath}\n\t" +
                $"CaToolVersion\t\t {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t {caToolInformation.CatoolRunningLocation}\n\t" +
                $"JFrogUrl:\t\t {appSettings.Jfrog.URL}\n\t" +
                $"Dry-run:\t\t {appSettings.Jfrog.DryRun}\n\t" +
                $"LogFolderPath:\t\t {Log4Net.CatoolLogPath}\n", null);

            //Validator method to check token validity
            ArtifactoryCredentials artifactoryCredentials = new ArtifactoryCredentials()
            {
                Token = appSettings.Jfrog.Token,
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication = new JfrogAqlApiCommunication(appSettings.Jfrog.URL, artifactoryCredentials, appSettings.TimeOut);
            ArtifactoryValidator artifactoryValidator = new(jfrogAqlApiCommunication);
            var isValid = await artifactoryValidator.ValidateArtifactoryCredentials();
            if (isValid == -1)
            {
                environmentHelper.CallEnvironmentExit(-1);
            }

            //Uploading Package to artifactory
            PackageUploadHelper.JFrogService = GetJfrogService(appSettings);
            UploadToArtifactory.JFrogService = GetJfrogService(appSettings);
            JfrogRepoUpdater.JFrogService = GetJfrogService(appSettings);
            await PackageUploader.UploadPackageToArtifactory(appSettings);

            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            if (appSettings.Telemetry.Enable == true)
            {
                TelemetryHelper telemetryHelper = new TelemetryHelper(appSettings);
                telemetryHelper.StartTelemetry(caToolInformation.CatoolVersion, PackageUploader.uploaderKpiData, TelemetryConstant.ArtifactoryUploaderKpiData);
            }
            Logger.Logger.Log(null, Level.Notice, $"End of Artifactory Uploader execution : {DateTime.Now}\n", null);
            // publish logs and BOM file to pipeline artifact

            PipelineArtifactUploader.UploadArtifacts();

        }

        private static CatoolInfo GetCatoolVersionFromProjectfile()
        {
            CatoolInfo catoolInfo = new CatoolInfo();
            var versionFromProj = Assembly.GetExecutingAssembly().GetName().Version;
            catoolInfo.CatoolVersion = $"{versionFromProj.Major}.{versionFromProj.Minor}.{versionFromProj.Build}";
            catoolInfo.CatoolRunningLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return catoolInfo;
        }        

        private static IJFrogService GetJfrogService(CommonAppSettings appSettings)
        {
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                Token = appSettings.Jfrog.Token
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.Jfrog.URL, artifactoryUpload, appSettings.TimeOut);
            IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(jfrogAqlApiCommunication);
            IJFrogService jFrogService = new JFrogService(jFrogApiCommunicationFacade);
            return jFrogService;
        }
    }
}
