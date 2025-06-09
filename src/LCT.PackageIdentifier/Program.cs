// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        private static IEnvironmentHelper environmentHelper = new EnvironmentHelper();

        private readonly ISettingsManager _settingsManager;
        private readonly IBomCreator _bomCreator;

        public Program(IFrameworkPackages frameworkPackages, ISettingsManager settingsManager, ICycloneDXBomParser cycloneDXBomParser, IBomCreator bomCreator)
        {
            _settingsManager = settingsManager;
            _bomCreator = bomCreator;
        }
        protected Program() { }

        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var program = serviceProvider.GetService<Program>();
            await program.Run(args);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IFrameworkPackages, FrameworkPackages>();
            services.AddTransient<ISettingsManager, SettingsManager>();
            services.AddTransient<ICycloneDXBomParser, CycloneDXBomParser>();
            services.AddTransient<IBomCreator, BomCreator>();
            services.AddTransient<Program>();
            services.AddScoped<ICompositionBuilder, CompositionBuilder>();
        }

        public async Task Run(string[] args)
        {
            BomStopWatch = new Stopwatch();
            BomStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;

            CommonAppSettings appSettings = _settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName);
            ProjectReleases projectReleases = new ProjectReleases();
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();
            Log4Net.CatoolCurrentDirectory = Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;
            LogFolderInitialisation(appSettings);

            _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifer");

            Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Package Identifier >>>>>====================", null);
            Logger.Logger.Log(null, Level.Notice, $"\nStart of Package Identifier execution: {DateTime.Now}", null);

            if (appSettings.ProjectType.ToUpperInvariant() == "ALPINE")
            {
                Logger.Error($"\nPlease note that the Alpine feature is currently in preview state. This means it's available for testing and evaluation purposes. While functional, it may not yet include all planned features and could encounter occasional issues. Your feedback during this preview phase is appreciated as we work towards its official release. Thank you for exploring Alpine with us.");
            }

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Alert, $"Package Identifier is running in TEST mode \n", null);

            // Validate application settings
            if (appSettings.SW360 != null)
            {
                await ValidateAppsettingsFile(appSettings, projectReleases);
            }
            string listOfInclude = DisplayInformation.DisplayIncludeFiles(appSettings);
            string listOfExclude = DisplayInformation.DisplayExcludeFiles(appSettings);
            string listOfExcludeComponents = DisplayInformation.DisplayExcludeComponents(appSettings);
            string listOfInternalRepoList = DisplayInformation.GetInternalRepolist(appSettings);

            DisplayInformation.LogInputParameters(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents);

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Notice, $"\tMode\t\t\t --> {appSettings.Mode}\n", null);

            _bomCreator.JFrogService = GetJfrogService(appSettings);
            _bomCreator.BomHelper = new BomHelper();

            //Validating JFrog Settings
            if (await _bomCreator.CheckJFrogConnection(appSettings))
            {
                await _bomCreator.GenerateBom(appSettings, new BomHelper(), new FileOperations(), projectReleases, caToolInformation);
            }

            if (appSettings?.Telemetry?.Enable == true)
            {
                TelemetryHelper telemetryHelper = new TelemetryHelper(appSettings);
                telemetryHelper.StartTelemetry(caToolInformation.CatoolVersion, BomCreator.bomKpiData, TelemetryConstant.IdentifierKpiData);
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
            if (appSettings == null)
            {
                return null;
            }
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                Token = appSettings.Jfrog?.Token,
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.Jfrog?.URL, artifactoryUpload, appSettings.TimeOut);
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
                environmentHelper.CallEnvironmentExit(-1);
            }
        }

        private static void LogFolderInitialisation(CommonAppSettings appSettings)
        {
            string FolderPath;
            if (!string.IsNullOrEmpty(appSettings.Directory.LogFolder))
            {
                FolderPath = appSettings.Directory.LogFolder;
                Log4Net.Init(FileConstant.BomCreatorLog, appSettings.Directory.LogFolder, m_Verbose);
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
        }
    }
}