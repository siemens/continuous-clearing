// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ArtifactoryUploader
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static bool m_Verbose = false;
        public static Stopwatch UploaderStopWatch { get; set; }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static async Task Main(string[] args)
        {
            UploaderStopWatch = new Stopwatch();
            UploaderStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;

            ISettingsManager settingsManager = new SettingsManager();
            CommonAppSettings appSettings = settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName);
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();

            Log4Net.CatoolCurrentDirectory = System.IO.Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;

            string FolderPath = InitiateLogger(appSettings);

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
                $"LogFolderPath:\t\t {Path.GetFullPath(FolderPath)}\n", null);

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
                CommonHelper.CallEnvironmentExit(-1);
            }

            //Uploading Package to artifactory
            PackageUploadHelper.jFrogService = GetJfrogService(appSettings);
            await PackageUploader.UploadPackageToArtifactory(appSettings);

            Logger.Logger.Log(null, Level.Notice, $"End of Artifactory Uploader execution : {DateTime.Now}\n", null);
            // publish logs and BOM file to pipeline artifact

            CommonHelper.PublishFilesToArtifact();

        }

        private static CatoolInfo GetCatoolVersionFromProjectfile()
        {
            CatoolInfo catoolInfo = new CatoolInfo();
            var versionFromProj = Assembly.GetExecutingAssembly().GetName().Version;
            catoolInfo.CatoolVersion = $"{versionFromProj.Major}.{versionFromProj.Minor}.{versionFromProj.Build}";
            catoolInfo.CatoolRunningLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return catoolInfo;
        }

        private static string InitiateLogger(CommonAppSettings appSettings)
        {
            string FolderPath;
            if (!string.IsNullOrEmpty(appSettings.LogFolderPath))
            {
                FolderPath = appSettings.LogFolderPath;
                Log4Net.Init(FileConstant.ArtifactoryUploaderLog, appSettings.LogFolderPath, m_Verbose);
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    FolderPath = FileConstant.LogFolder;
                }
                else
                {
                    FolderPath = "/var/log";
                }
                Log4Net.Init(FileConstant.ArtifactoryUploaderLog, FolderPath, m_Verbose);
            }

            return FolderPath;
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
