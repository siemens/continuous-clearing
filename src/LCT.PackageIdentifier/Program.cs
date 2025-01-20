// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Facade;
using LCT.Facade.Interfaces;
using LCT.PackageIdentifier.Interface;
using LCT.Services;
using LCT.Services.Interface;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Telemetry;


namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Program class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static bool m_Verbose = false;

        public static Stopwatch BomStopWatch { get; set; }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Program() { }

        static async Task Main(string[] args)
        {
            BomStopWatch = new Stopwatch();
            BomStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;
            ISettingsManager settingsManager = new SettingsManager();
            CommonAppSettings appSettings = settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName);
            ProjectReleases projectReleases = new ProjectReleases();
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();
            Log4Net.CatoolCurrentDirectory = Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;
            string FolderPath = LogFolderInitialisation(appSettings);

            settingsManager.CheckRequiredArgsToRun(appSettings, "Identifer");

            Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Package Identifier >>>>>====================", null);
            Logger.Logger.Log(null, Level.Notice, $"\nStart of Package Identifier execution: {DateTime.Now}", null);

            if (appSettings.ProjectType.ToUpperInvariant() == "ALPINE")
            {
                Logger.Error($"\nPlease note that the Alpine feature is currently in preview state. This means it's available for testing and evaluation purposes. While functional, it may not yet include all planned features and could encounter occasional issues. Your feedback during this preview phase is appreciated as we work towards its official release. Thank you for exploring Alpine with us.");
            }

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Alert, $"Package Identifier is running in TEST mode \n", null);

            // Validate application settings
            await ValidateAppsettingsFile(appSettings, projectReleases);
            string listOfInlude = DisplayInclude(appSettings);
            string listOfExclude = DisplayExclude(appSettings);
            string listOfExcludeComponents = DisplayExcludeComponents(appSettings);
            string listOfInternalRepoList = string.Empty;
            if (appSettings.InternalRepoList != null)
            {
                listOfInternalRepoList = string.Join(",", appSettings.InternalRepoList?.ToList());
            }

            Logger.Logger.Log(null, Level.Notice, $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.PackageFilePath}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.BomFolderPath}\n\t" +
                $"SBOMTemplateFilePath\t --> {appSettings.CycloneDxSBomTemplatePath}\n\t" +
                $"SW360Url\t\t --> {appSettings.SW360URL}\n\t" +
                $"SW360AuthTokenType\t --> {appSettings.SW360AuthTokenType}\n\t" +
                $"SW360ProjectName\t --> {appSettings.SW360ProjectName}\n\t" +
                $"SW360ProjectID\t\t --> {appSettings.SW360ProjectID}\n\t" +
                $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                $"InternalRepoList\t --> {listOfInternalRepoList}\n\t" +
                $"Include\t\t\t --> {listOfInlude}\n\t" +
                $"Exclude\t\t\t --> {listOfExclude}\n\t" +
                $"ExcludeComponents\t --> {listOfExcludeComponents}\n", null);


            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Notice, $"\tMode\t\t\t --> {appSettings.Mode}\n", null);

            ICycloneDXBomParser cycloneDXBomParser = new CycloneDXBomParser();
            IBomCreator bomCreator = new BomCreator(cycloneDXBomParser);
            bomCreator.JFrogService = GetJfrogService(appSettings);
            bomCreator.BomHelper = new BomHelper();

            //Validating JFrog Settings
            if (await bomCreator.CheckJFrogConnection())
            {
                await bomCreator.GenerateBom(appSettings, new BomHelper(), new FileOperations(), projectReleases,
                                             caToolInformation);
            }
            Logger.Logger.Log(null, Level.Notice, $"End of Package Identifier execution : {DateTime.Now}\n", null);

            // publish logs and bom file to pipeline artifact
            CommonHelper.PublishFilesToArtifact();

            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            if (appSettings.Telemetry == true)
            {
                Logger.Logger.Log(null, Level.Notice, $"\nStart of Package Identifier Telemtery execution: {DateTime.Now}", null);
                Telemetry.Telemetry telemetry = new Telemetry.Telemetry("ApplicationInsights", new Dictionary<string, string>
            {
                { "InstrumentationKey", appSettings.ApplicationInsight_InstrumentKey }
            });
                try
                {
                    telemetry.Initialize("CATool", caToolInformation.CatoolVersion);

                    telemetry.TrackCustomEvent("PackageIdentifierExecution", new Dictionary<string, string>
            {
                { "CA Tool Version", caToolInformation.CatoolVersion },
                { "SW360 Project Name", appSettings.SW360ProjectName },
                { "SW360 Project ID", appSettings.SW360ProjectID },
                { "Project Type", appSettings.ProjectType },
                 { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
                    // Track KPI data if available
                    if (BomCreator.bomKpiData != null)
                    {
                        telemetry.TrackCustomEvent("BomKpiDataTelemetry", new Dictionary<string, string>
                    
                        {
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Components In Input File", BomCreator.bomKpiData.ComponentsinPackageLockJsonFile.ToString() },
                { "Dev Dependent Components", BomCreator.bomKpiData.DevDependentComponents.ToString() },
                { "Bundled Dependent Components", BomCreator.bomKpiData.BundledComponents.ToString() },
                { "Total Duplicate Components", BomCreator.bomKpiData.DuplicateComponents.ToString() },
                { "Internal Components Identified", BomCreator.bomKpiData.InternalComponents.ToString() },
                { "Components already present in 3rd party repo(s)", BomCreator.bomKpiData.ThirdPartyRepoComponents.ToString() },
                { "Components already present in devdep repo(s)", BomCreator.bomKpiData.DevdependencyComponents.ToString() },
                { "Components already present in release repo(s)", BomCreator.bomKpiData.ReleaseRepoComponents.ToString() },
                { "Components not from official repo(s)", BomCreator.bomKpiData.UnofficialComponents.ToString() },
                { "Total Components Excluded", BomCreator.bomKpiData.ComponentsExcluded.ToString() },
                { "Components With SourceURL", BomCreator.bomKpiData.ComponentsWithSourceURL.ToString() },
                { "Components In Comparison BOM", BomCreator.bomKpiData.ComponentsInComparisonBOM.ToString() },
                { "Time taken by BOM Creator", BomCreator.bomKpiData.TimeTakenByBomCreator.ToString() },
                { "Components Added From SBOM Template", BomCreator.bomKpiData.ComponentsinSBOMTemplateFile.ToString() },
                { "Components Updated From SBOM Template", BomCreator.bomKpiData.ComponentsUpdatedFromSBOMTemplateFile.ToString() },
                { "Project Summary Link", BomCreator.bomKpiData.ProjectSummaryLink },
                { "Time stamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
                    }

                    telemetry.TrackExecutionTime();
                    Logger.Logger.Log(null, Level.Notice, $"End of Package Identifier Telemetry execution : {DateTime.Now}\n", null);
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

        private static async Task ValidateAppsettingsFile(CommonAppSettings appSettings, ProjectReleases projectReleases)
        {
            SW360ConnectionSettings sw360ConnectionSettings = new SW360ConnectionSettings()
            {
                SW360URL = appSettings.SW360URL,
                SW360AuthTokenType = appSettings.SW360AuthTokenType,
                Sw360Token = appSettings.Sw360Token,
                IsTestMode = appSettings.IsTestMode,
                Timeout = appSettings.TimeOut
            };
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(new SW360ApicommunicationFacade(sw360ConnectionSettings));
            int isValid = await BomValidator.ValidateAppSettings(appSettings, sw360ProjectService, projectReleases);
            if (isValid == -1)
            {
                CommonHelper.CallEnvironmentExit(-1);
            }
        }
        private static string DisplayInclude(CommonAppSettings appSettings)
        {
            string totalString = string.Empty;
            switch (appSettings.ProjectType.ToUpperInvariant())
            {
                case "NPM":
                    if (appSettings.Npm.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Npm.Include?.ToList());
                    }
                    return totalString;
                case "NUGET":
                    if (appSettings.Nuget.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Nuget.Include?.ToList());
                    }
                    return totalString;
                case "MAVEN":
                    if (appSettings.Maven.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Maven.Include?.ToList());
                    }
                    return totalString;
                case "DEBIAN":
                    if (appSettings.Debian.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Debian.Include?.ToList());
                    }

                    return totalString;
                case "PYTHON":
                    if (appSettings.Python.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Python.Include?.ToList());
                    }
                    return totalString;
                case "CONAN":
                    if (appSettings.Conan.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Conan.Include?.ToList());
                    }
                    return totalString;
                case "ALPINE":
                    if (appSettings.Alpine.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Alpine.Include?.ToList());
                    }
                    return totalString;
                default:
                    Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
                    break;
            }
            return totalString;
        }
        private static string DisplayExclude(CommonAppSettings appSettings)
        {

            string totalString = string.Empty;
            switch (appSettings.ProjectType.ToUpperInvariant())
            {
                case "NPM":
                    if (appSettings.Npm.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Npm.Exclude?.ToList());
                    }
                    return totalString;
                case "NUGET":
                    if (appSettings.Nuget.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Nuget.Exclude?.ToList());
                    }
                    return totalString;
                case "MAVEN":
                    if (appSettings.Maven.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Maven.Exclude?.ToList());
                    }
                    return totalString;
                case "DEBIAN":
                    if (appSettings.Debian.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Debian.Exclude?.ToList());
                    }
                    return totalString;
                case "PYTHON":
                    if (appSettings.Python.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Python.Exclude?.ToList());
                    }
                    return totalString;
                case "CONAN":
                    if (appSettings.Conan.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Conan.Exclude?.ToList());
                    }
                    return totalString;
                case "ALPINE":
                    if (appSettings.Alpine.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Alpine.Include?.ToList());
                    }
                    return totalString;
                default:
                    Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
                    break;
            }
            return totalString;
        }

        private static string DisplayExcludeComponents(CommonAppSettings appSettings)
        {

            string totalString = string.Empty;
            switch (appSettings.ProjectType.ToUpperInvariant())
            {
                case "NPM":
                    if (appSettings.Npm.ExcludedComponents != null)
                    {
                        totalString = string.Join(",", appSettings.Npm.ExcludedComponents?.ToList());
                    }
                    return totalString;
                case "NUGET":
                    if (appSettings.Nuget.ExcludedComponents != null)
                    {
                        totalString = string.Join(",", appSettings.Nuget.ExcludedComponents?.ToList());
                    }
                    return totalString;
                case "MAVEN":
                    if (appSettings.Maven.ExcludedComponents != null)
                    {
                        totalString = string.Join(",", appSettings.Maven.ExcludedComponents?.ToList());
                    }
                    return totalString;
                case "DEBIAN":
                    if (appSettings.Debian.ExcludedComponents != null)
                    {
                        totalString = string.Join(",", appSettings.Debian.ExcludedComponents?.ToList());
                    }

                    return totalString;
                case "PYTHON":
                    if (appSettings.Python.ExcludedComponents != null)
                    {
                        totalString = string.Join(",", appSettings.Python.ExcludedComponents?.ToList());
                    }
                    return totalString;
                case "CONAN":
                    if (appSettings.Conan.ExcludedComponents != null)
                    {
                        totalString = string.Join(",", appSettings.Conan.ExcludedComponents?.ToList());
                    }
                    return totalString;
                case "ALPINE":
                    if (appSettings.Alpine.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Alpine.Include?.ToList());
                    }
                    return totalString;
                default:
                    Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
                    break;
            }
            return totalString;
        }

        private static string LogFolderInitialisation(CommonAppSettings appSettings)
        {
            string FolderPath;
            if (!string.IsNullOrEmpty(appSettings.LogFolderPath))
            {
                FolderPath = appSettings.LogFolderPath;
                Log4Net.Init(FileConstant.BomCreatorLog, appSettings.LogFolderPath, m_Verbose);
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

                Log4Net.Init(FileConstant.BomCreatorLog, FolderPath, m_Verbose);
            }

            return FolderPath;
        }
    }
}
