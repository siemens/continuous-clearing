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
using LCT.Facade.Interfaces;
using LCT.Facade;
using LCT.Services.Interface;
using LCT.Services;
using log4net;
using log4net.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LCT.Common.Model;
using System.Diagnostics.CodeAnalysis;
using LCT.ArtifactoryUploader.Model;
using System.Collections.Generic;
using System.Globalization;
using Telemetry;


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

            Log4Net.CatoolCurrentDirectory = Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;

            string FolderPath = InitiateLogger(appSettings);

            settingsManager.CheckRequiredArgsToRun(appSettings, "Uploader");

            Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Artifactory Uploader >>>>>====================", null);
            Logger.Logger.Log(null, Level.Notice, $"\nStart of Artifactory Uploader execution: {DateTime.Now}", null);

            if (appSettings.Release)
                Logger.Logger.Log(null, Level.Alert, $"Artifactory Uploader is running in release mode !!! \n", null);
            else
                Logger.Logger.Log(null, Level.Alert, $"Artifactory Uploader is running in dry-run mode, no packages will be moved \n", null);

            Logger.Logger.Log(null, Level.Info, $"Input Parameters used in Artifactory Uploader:\n\t", null);
            Logger.Logger.Log(null, Level.Notice, $"\tBomFilePath:\t\t {appSettings.BomFilePath}\n\t" +
                $"CaToolVersion\t\t {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t {caToolInformation.CatoolRunningLocation}\n\t" +
                $"JFrogUrl:\t\t {appSettings.JFrogApi}\n\t" +
                $"Release:\t\t {appSettings.Release}\n\t" +
                $"LogFolderPath:\t\t {Path.GetFullPath(FolderPath)}\n", null);

            //Validator method to check token validity
            ArtifactoryCredentials artifactoryCredentials = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey,
                Email = appSettings.ArtifactoryUploadUser
            };
            NpmJfrogApiCommunication jfrogCommunication = new NpmJfrogApiCommunication(appSettings.JFrogApi, appSettings.JfrogNpmSrcRepo, artifactoryCredentials, appSettings.TimeOut);
            ArtifactoryValidator artifactoryValidator = new(jfrogCommunication);
            var isValid = await artifactoryValidator.ValidateArtifactoryCredentials(appSettings);
            if (isValid == -1)
            {
                CommonHelper.CallEnvironmentExit(-1);
            }

            //Uploading Package to artifactory
            PackageUploadHelper.jFrogService = GetJfrogService(appSettings);
            await PackageUploader.UploadPackageToArtifactory(appSettings);

            Logger.Logger.Log(null, Level.Notice, $"End of Artifactory Uploader execution : {DateTime.Now}\n", null);
            // publish logs and bom file to pipeline artifact

            CommonHelper.PublishFilesToArtifact();
            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            if (appSettings.Telemetry == true)
            {
                Logger.Logger.Log(null, Level.Notice, $"\nStart of Package Identifier Telemetry execution: {DateTime.Now}", null);
                Telemetry.Telemetry telemetry = new Telemetry.Telemetry("ApplicationInsights", new Dictionary<string, string>
                {
                    { "InstrumentationKey", appSettings.ApplicationInsight_InstrumentKey }
                });

                try
                {
                    telemetry.Initialize("CATool", caToolInformation.CatoolVersion);

                    telemetry.TrackCustomEvent("ArtifactoryUploaderExecution", new Dictionary<string, string>
                    {
                        { "CA Tool Version", caToolInformation.CatoolVersion },
                        { "SW360 Project Name", appSettings.SW360ProjectName },
                        { "SW360 Project ID", appSettings.SW360ProjectID },
                        { "Project Type", appSettings.ProjectType },
                        { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                        { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
                    });

                    // Track KPI data if available
                    if (PackageUploader.uploaderKpiData != null)
                    {
                        telemetry.TrackCustomEvent("UploaderKpiDataTelemetry", new Dictionary<string, string>
                        {
                            { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                            { "Components In Comparison BOM", PackageUploader.uploaderKpiData.ComponentInComparisonBOM.ToString() },
                            { "Packages in Not Approved State", PackageUploader.uploaderKpiData .ComponentNotApproved.ToString() },
                            { "Packages in Approved State", PackageUploader.uploaderKpiData .PackagesToBeUploaded.ToString() },
                            { "Packages Copied to Siparty Repo", PackageUploader.uploaderKpiData.PackagesUploadedToJfrog.ToString() },
                            { "Packages Not Copied to Siparty Repo", PackageUploader.uploaderKpiData.PackagesNotUploadedToJfrog.ToString() },
                            { "Packages Not Existing in Repository", PackageUploader.uploaderKpiData.PackagesNotExistingInRemoteCache.ToString() },
                            { "Packages Not Actioned Due To Error", PackageUploader.uploaderKpiData.PackagesNotUploadedDueToError.ToString() },
                            { "Time taken by ComponentCreator", PackageUploader.uploaderKpiData.TimeTakenByComponentCreator.ToString() },
                            { "Development Packages to be Copied to Siparty DevDep Repo", PackageUploader.uploaderKpiData.DevPackagesToBeUploaded.ToString() },
                            { "Development Packages Copied to Siparty DevDep Repo", PackageUploader.uploaderKpiData.DevPackagesUploaded.ToString() },
                            { "Development Packages Not Copied to Siparty DevDep Repo", PackageUploader.uploaderKpiData.DevPackagesNotUploadedToJfrog.ToString() },
                            { "Internal Packages to be Moved", PackageUploader.uploaderKpiData.InternalPackagesToBeUploaded.ToString() },
                            { "Internal Packages Moved to Repo", PackageUploader.uploaderKpiData.InternalPackagesUploaded.ToString() },
                            { "Internal Packages Not Moved to Repo", PackageUploader.uploaderKpiData.InternalPackagesNotUploadedToJfrog.ToString() },
                            { "Time stamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
                        });
                    }
                    telemetry.TrackExecutionTime();
                    Logger.Logger.Log(null, Level.Notice, $"End of Artifactory Uploader Telemetry execution : {DateTime.Now}\n", null);
                }
                catch (Exception ex)
                {
                    Logger.Error($"An error occurred: {ex.Message}");
                    telemetry.TrackException(ex, new Dictionary<string, string>
                    {
                        { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                        { "Stack Trace", ex.StackTrace }
                    });
                    CommonHelper.CallEnvironmentExit(-1);
                }
                finally
                {
                    telemetry.Flush(); // Ensure telemetry is sent before application exits
                }
            }

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
                ApiKey = appSettings.ArtifactoryUploadApiKey
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.JFrogApi, artifactoryUpload, appSettings.TimeOut);
            IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(jfrogAqlApiCommunication);
            IJFrogService jFrogService = new JFrogService(jFrogApiCommunicationFacade);
            return jFrogService;
        }
    }
}
