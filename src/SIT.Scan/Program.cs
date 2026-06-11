// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Bom SIT

using log4net;
using log4net.Core;
using Microsoft.Extensions.DependencyInjection;
using SIT.APICommunications;
using SIT.APICommunications.Interfaces;
using SIT.APICommunications.Model;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Interface;
using SIT.Common.Logging;
using SIT.Common.Model;
using SIT.Facade;
using SIT.Facade.Interfaces;
using SIT.Scan.Interface;
using SIT.Services;
using SIT.Services.Interface;
using SW360KeycloakService;
using SW360KeycloakService.Model;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace SIT.Scan
{
    /// <summary>
    /// Program class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        #region Fields
        private bool m_Verbose = false;

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();

        private readonly ISettingsManager _settingsManager;
        private readonly IBomCreator _bomCreator;
        #endregion

        #region Properties
        public static Stopwatch BomStopWatch { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Program"/> class with provided dependencies.
        /// </summary>
        /// <param name="frameworkPackages">Framework packages provider.</param>
        /// <param name="settingsManager">Settings manager instance.</param>
        /// <param name="cycloneDXBomParser">CycloneDX BOM parser.</param>
        /// <param name="bomCreator">BOM creator instance.</param>
        public Program(IFrameworkPackages frameworkPackages, ISettingsManager settingsManager, ICycloneDXBomParser cycloneDXBomParser, IBomCreator bomCreator)
        {
            _settingsManager = settingsManager;
            _bomCreator = bomCreator;
        }

        /// <summary>
        /// Protected parameterless constructor for testing or DI.
        /// </summary>
        protected Program() { }
        #endregion

        #region Methods
        /// <summary>
        /// Asynchronously entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Asynchronously completes when the application finishes.</returns>
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var program = serviceProvider.GetService<Program>();
            await program.Run(args);
        }

        /// <summary>
        /// Configures dependency injection services for the application.
        /// </summary>
        /// <param name="services">Service collection to populate.</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IFrameworkPackages, FrameworkPackages>();
            services.AddTransient<ISettingsManager, SettingsManager>();
            services.AddTransient<ICycloneDXBomParser, CycloneDXBomParser>();
            services.AddTransient<IBomCreator, BomCreator>();
            services.AddTransient<ISpdxBomParser, SpdxBomParser>();
            services.AddTransient<IEnvironmentHelper, EnvironmentHelper>();
            services.AddTransient<Program>();
            services.AddScoped<ICompositionBuilder, CompositionBuilder>();
            services.AddScoped<IRuntimeIdentifier, DotnetRuntimeIdentifer>();
        }

        /// <summary>
        /// Asynchronously runs the main program logic using the provided arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Asynchronously completes when run finishes.</returns>
        public async Task Run(string[] args)
        {
            BomStopWatch = new Stopwatch();
            BomStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();
            Log4Net.CatoolCurrentDirectory = Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;
            string logFileNameWithTimestamp = $"{FileConstant.SITScanLog}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            CommonHelper.DefaultLogFolderInitialization(logFileNameWithTimestamp, m_Verbose);
            Logger.Debug($"====================<<<<< SIT Scan >>>>>====================");
            CommonAppSettings appSettings = _settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName, environmentHelper);
            Log4Net.AppendVerboseValue(appSettings);
            appSettings.ProjectType = CommonHelper.CanonicalizeProjectType(appSettings.ProjectType);
            ProjectReleases projectReleases = new ProjectReleases();
            string _ = CommonHelper.LogFolderInitialization(appSettings, logFileNameWithTimestamp, m_Verbose);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            _settingsManager.CheckRequiredArgsToRun(appSettings, Dataconstant.Scan);
            LoggerHelper.SpectreConsoleInitialMessage("SIT Scan");
            if (appSettings.ProjectType.Equals("ALPINE", StringComparison.InvariantCultureIgnoreCase))
            {
                Logger.Error($"\nPlease note that the Alpine feature is currently in preview state. This means it's available for testing and evaluation purposes. While functional, it may not yet include all planned features and could encounter occasional issues. Your feedback during this preview phase is appreciated as we work towards its official release. Thank you for exploring Alpine with us.");
            }

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Alert, $"SIT Scan is running in TEST mode \n", null);

            // Validate application settings
            if (appSettings.SW360 != null)
            {
                CommonHelper.DisplayTokenExpiryWarning(appSettings);
                await ValidateAppsettingsFile(appSettings, projectReleases);
            }

            var listParameters = new ListofPerametersForCli
            {
                InternalRepoList = DisplayInformation.GetInternalRepolist(appSettings),
                Include = DisplayInformation.DisplayIncludeFiles(appSettings),
                Exclude = DisplayInformation.DisplayExcludeFiles(appSettings),
                ExcludeComponents = DisplayInformation.DisplayExcludeComponents(appSettings)
            };
            LoggerHelper.LogInputParameters(caToolInformation, appSettings, listParameters, Dataconstant.Scan);

            _bomCreator.JFrogService = GetJfrogService(appSettings);
            _bomCreator.BomHelper = new BomHelper();

            //Validating JFrog Settings
            if (await _bomCreator.CheckJFrogConnection(appSettings))
            {
                await _bomCreator.GenerateBom(appSettings, new BomHelper(), new FileOperations(), projectReleases, caToolInformation);
            }

            if (appSettings.Telemetry?.Enable == true)
            {
                TelemetryHelper telemetryHelper = new TelemetryHelper(appSettings);
                telemetryHelper.StartTelemetry(caToolInformation.CatoolVersion, BomCreator.bomKpiData, TelemetryConstant.ScanKpiData);
            }
            Logger.Logger.Log(null, Level.Notice, $"End of SIT Scan execution : {DateTime.Now}\n", null);
            // publish logs and bom file to pipeline artifact
            PipelineArtifactUploader.UploadArtifacts();
        }

        /// <summary>
        /// Retrieves the CA tool version and running location from the executing assembly.
        /// </summary>
        /// <returns>Information about the CA tool version and running location.</returns>
        private static CatoolInfo GetCatoolVersionFromProjectfile()
        {
            CatoolInfo catoolInfo = new CatoolInfo();
            var versionFromProj = Assembly.GetExecutingAssembly().GetName().Version;
            catoolInfo.CatoolVersion = $"{versionFromProj.Major}.{versionFromProj.Minor}.{versionFromProj.Build}";
            catoolInfo.CatoolRunningLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return catoolInfo;
        }

        /// <summary>
        /// Creates an IJFrogService instance configured from the given application settings.
        /// </summary>
        /// <param name="appSettings">Application settings that contain JFrog configuration.</param>
        /// <returns>Configured IJFrogService or null when settings are not provided.</returns>
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

        /// <summary>
        /// Asynchronously validates SW360 settings from application settings and exits the environment on failure.
        /// </summary>
        /// <param name="appSettings">Application settings to validate.</param>
        /// <param name="projectReleases">Project releases holder used during validation.</param>
        /// <returns>Asynchronously completes after validation (may exit process on failure).</returns>
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
            KeycloakTokenCacheService tokenService = await InitializeKeycloakTokenServiceAsync(appSettings, sw360ConnectionSettings);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(new SW360ApicommunicationFacade(sw360ConnectionSettings, tokenService));
            int isValid = await BomValidator.ValidateAppSettings(appSettings, sw360ProjectService, projectReleases);
            if (isValid == -1)
            {
                environmentHelper.CallEnvironmentExit(-1);
            }
        }

        /// <summary>
        /// Initializes the Keycloak token service if <c>ClientId</c> and <c>ClientSecret</c> are configured,
        /// fetches a fresh token, and syncs the result back to <paramref name="appSettings"/> and
        /// <paramref name="sw360ConnectionSettings"/>. Returns <c>null</c> when Keycloak credentials are absent.
        /// </summary>
        private static async Task<KeycloakTokenCacheService> InitializeKeycloakTokenServiceAsync(
            CommonAppSettings appSettings, SW360ConnectionSettings sw360ConnectionSettings)
        {
            if (!CommonHelper.ValidateKeycloakCredentials(appSettings, environmentHelper.CallEnvironmentExit))
            {
                return null;
            }

            var tokenSettings = new TokenServiceSettings
            {
                SW360BaseUrl = appSettings.SW360.URL,
                ClientId = appSettings.SW360.ClientId,
                ClientSecret = appSettings.SW360.ClientSecret,
                KeyCloakToken = appSettings.SW360.Token,
                KeyCloakTokenType = appSettings.SW360.AuthTokenType
            };
            var tokenService = new KeycloakTokenCacheService(tokenSettings, environmentHelper.CallEnvironmentExit);
            await tokenService.GetOrRefreshTokenAsync();
            appSettings.SW360.Token = tokenSettings.KeyCloakToken;
            appSettings.SW360.AuthTokenType = tokenSettings.KeyCloakTokenType;
            sw360ConnectionSettings.Sw360Token = appSettings.SW360.Token;
            sw360ConnectionSettings.SW360AuthTokenType = appSettings.SW360.AuthTokenType;
            return tokenService;
        }
        #endregion

        #region Events
        #endregion
    }
}