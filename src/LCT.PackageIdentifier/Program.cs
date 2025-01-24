// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Facade;
using LCT.Services;
using LCT.Services.Interface;
using LCT.PackageIdentifier.Interface;
using log4net;
using log4net.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Directory = System.IO.Directory;


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
            string listOfInternalRepoList = GetInternalRepolist(appSettings);

            Logger.Logger.Log(null, Level.Notice, $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t" +
                $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                $"SW360AuthTokenType\t --> {appSettings.SW360.AuthTokenType}\n\t" +
                $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
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
                ApiKey = appSettings.Jfrog.Token,
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.Jfrog.URL, artifactoryUpload, appSettings.TimeOut);
            IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(jfrogAqlApiCommunication);
            IJFrogService jFrogService = new JFrogService(jFrogApiCommunicationFacade);
            return jFrogService;
        }

        private static async Task ValidateAppsettingsFile(CommonAppSettings appSettings, ProjectReleases projectReleases)
        {
            SW360ConnectionSettings sw360ConnectionSettings = new SW360ConnectionSettings()
            {
                SW360URL = appSettings.SW360.URL,
                SW360AuthTokenType = appSettings.SW360.AuthTokenType,
                Sw360Token = appSettings.SW360.Token,
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
                case "POETRY":
                    if (appSettings.Poetry.Include != null)
                    {
                        totalString = string.Join(",", appSettings.Poetry.Include?.ToList());
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
                case "POETRY":
                    if (appSettings.Poetry.Exclude != null)
                    {
                        totalString = string.Join(",", appSettings.Poetry.Exclude?.ToList());
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
            if (appSettings.SW360.ExcludeComponents != null)
            {
                totalString = string.Join(",", appSettings.SW360.ExcludeComponents?.ToList());
            }
            return totalString;
        }

        private static string GetInternalRepolist(CommonAppSettings appSettings)
        {
            string listOfInternalRepoList = string.Empty;

            var repoMapping = new Dictionary<string, Func<IEnumerable<string>>>(StringComparer.OrdinalIgnoreCase)
        {
        { "NPM", () => appSettings.Npm?.Artifactory.InternalRepos },
        { "NUGET", () => appSettings.Nuget?.Artifactory.InternalRepos },
        { "MAVEN", () => appSettings.Maven?.Artifactory.InternalRepos },
        { "DEBIAN", () => appSettings.Debian?.Artifactory.InternalRepos },
        { "POETRY", () => appSettings.Poetry?.Artifactory.InternalRepos },
        { "CONAN", () => appSettings.Conan?.Artifactory.InternalRepos },
        { "ALPINE", () => appSettings.Alpine?.Artifactory.InternalRepos }
        };

            if (repoMapping.TryGetValue(appSettings.ProjectType, out var getRepos))
            {
                var repos = getRepos();
                if (repos != null)
                {
                    listOfInternalRepoList = string.Join(",", repos);
                }
            }
            else
            {
                Logger.Error($"Invalid ProjectType - {appSettings.ProjectType}");
            }

            return listOfInternalRepoList;
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
