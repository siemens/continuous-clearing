// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.ComplianceValidator;
using LCT.Common.Constants;
using LCT.Common.Logging;
using LCT.Common.Model;
using LCT.Facade;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Interfaces;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// Program class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static bool m_Verbose = false;
        public static Stopwatch CreatorStopWatch { get; set; }
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        private static List<ComparisonBomData> parsedBomData;

        protected Program() { }

        /// <summary>
        /// Initializes and executes the package creator application workflow. This includes reading configuration
        /// settings, validating input parameters, performing compliance checks, and uploading artifacts as part of the
        /// application's main entry point.
        /// </summary>        
        /// <param name="args">An array of command-line arguments supplied to the application. These arguments are used to configure
        /// application settings and control execution behavior.</param>
        /// <returns>A task that represents the asynchronous operation of the application's main workflow.</returns>
        static async Task Main(string[] args)
        {
            CreatorStopWatch = new Stopwatch();
            CreatorStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;

            SettingsManager settingsManager = new SettingsManager();
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();
            Log4Net.CatoolCurrentDirectory = Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;
            string logFileNameWithTimestamp = $"{FileConstant.ComponentCreatorLog}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            CommonHelper.DefaultLogFolderInitialization(logFileNameWithTimestamp, m_Verbose);
            Logger.Debug($"====================<<<<< Package creator >>>>>====================");
            CommonAppSettings appSettings = settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName, environmentHelper);
            Log4Net.AppendVerboseValue(appSettings);

            ISw360ProjectService sw360ProjectService = Getsw360ProjectServiceObject(appSettings, out ISW360ApicommunicationFacade sW360ApicommunicationFacade);
            ProjectReleases projectReleases = new ProjectReleases();

            string FolderPath = CommonHelper.LogFolderInitialization(appSettings, logFileNameWithTimestamp, m_Verbose);
            Logger.Logger.Log(null, Level.Debug, $"log manager initiated folder name: {FolderPath}", null);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            settingsManager.CheckRequiredArgsToRun(appSettings, Dataconstant.Creator);
            int isValid = await CreatorValidator.ValidateAppSettings(appSettings, sw360ProjectService, projectReleases);

            if (isValid == -1)
            {
                environmentHelper.CallEnvironmentExit(-1);
            }
            LoggerHelper.SpectreConsoleInitialMessage("Package creator");

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Alert, $"Package creator is running in TEST mode \n", null);
            var bomFilePath = Path.Combine(appSettings.Directory.OutputFolder, appSettings.SW360.ProjectName + "_" + FileConstant.BomFileName);
            ListofPerametersForCli listofPerameters = new ListofPerametersForCli();
            LoggerHelper.LogInputParameters(caToolInformation, appSettings, listofPerameters, exeType: Dataconstant.Creator, bomFilePath: bomFilePath);

            //Validate Fossology Url
            if (appSettings.SW360.Fossology.EnableTrigger && !appSettings.IsTestMode)
            {
                HttpClient client = new HttpClient();
                if (await CreatorValidator.FossologyUrlValidation(appSettings, client, environmentHelper))
                    await CreatorValidator.TriggerFossologyValidation(appSettings, sW360ApicommunicationFacade, environmentHelper);
            }
            await InitiatePackageCreatorProcess(appSettings, sw360ProjectService, sW360ApicommunicationFacade);

            //Look for Compliance exceptions and print them with warnings 
            await ComplianceCheckForAllFoundComponents();

            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            if (appSettings.Telemetry.Enable)
            {
                TelemetryHelper telemetryHelper = new TelemetryHelper(appSettings);
                telemetryHelper.StartTelemetry(caToolInformation.CatoolVersion, ComponentCreator.KpiData, TelemetryConstant.CreatorKpiData);
            }
            Logger.Logger.Log(null, Level.Notice, $"End of Package Creator execution: {DateTime.Now}\n", null);

            // publish logs and bom file to pipeline artifact
            PipelineArtifactUploader.UploadArtifacts();
        }

        /// <summary>
        /// gets the catool version from project file
        /// </summary>
        /// <returns>ca tool information</returns>
        private static CatoolInfo GetCatoolVersionFromProjectfile()
        {
            CatoolInfo catoolInfo = new CatoolInfo();
            var versionFromProj = Assembly.GetExecutingAssembly().GetName().Version;
            catoolInfo.CatoolVersion = $"{versionFromProj.Major}.{versionFromProj.Minor}.{versionFromProj.Build}";
            catoolInfo.CatoolRunningLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return catoolInfo;
        }

        /// <summary>
        /// gets the sw360 project service object
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="sW360ApicommunicationFacade"></param>
        /// <returns>project service data</returns>
        private static ISw360ProjectService Getsw360ProjectServiceObject(CommonAppSettings appSettings, out ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ISw360ProjectService sw360ProjectService;
            SW360ConnectionSettings sw360ConnectionSettings = new SW360ConnectionSettings()
            {
                SW360URL = appSettings.SW360.URL,
                SW360AuthTokenType = appSettings.SW360.AuthTokenType,
                Sw360Token = appSettings.SW360.Token,
                IsTestMode = appSettings.IsTestMode,
                Timeout = appSettings.TimeOut
            };


            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(sw360ConnectionSettings);
            sw360ProjectService = new Sw360ProjectService(sW360ApicommunicationFacade);
            return sw360ProjectService;
        }

        /// <summary>
        /// initiates the package creator process
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="sw360ProjectService"></param>
        /// <param name="sW360ApicommunicationFacade"></param>
        /// <returns>a task represents async operation</returns>
        private static async Task InitiatePackageCreatorProcess(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService, ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ISW360CommonService sw360CommonService = new SW360CommonService(sW360ApicommunicationFacade);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sW360ApicommunicationFacade, sw360CommonService);
            ISW360Service sw360Service = new Sw360Service(sW360ApicommunicationFacade, sw360CommonService, environmentHelper);
            ICycloneDXBomParser cycloneDXBomParser = new CycloneDXBomParser();

            IDebianPatcher debianPatcher = new DebianPatcher();
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>
            {
                { "NPM", new PackageDownloader() },
                { "NUGET", new PackageDownloader() },
                { "DEBIAN", new DebianPackageDownloader(debianPatcher) },
                { "ALPINE", new AlpinePackageDownloader() }
            };

            ICreatorHelper creatorHelper = new CreatorHelper(_packageDownloderList);

            // parsing the input file
            ComponentCreator componentCreator = new ComponentCreator();
            parsedBomData = await componentCreator.CycloneDxBomParser(appSettings, sw360Service, cycloneDXBomParser, creatorHelper);

            // initializing Component creation 
            await componentCreator.CreateComponentInSw360(appSettings, sw360CreatorService, sw360Service,
                 sw360ProjectService, new FileOperations(), creatorHelper, parsedBomData);
        }

        /// <summary>
        /// compliance check for all found components
        /// </summary>
        /// <returns>task that represents asynchronous operation</returns>
        private static async Task ComplianceCheckForAllFoundComponents()
        {
            if (parsedBomData != null && parsedBomData.Count > 0)
            {
                ComplianceCheck compliance = new ComplianceCheck();
                ComplianceSettingsModel complianceSettings = new();
                string baseDir = AppContext.BaseDirectory;
                string[] foundFiles = Directory.GetFiles(baseDir, "ComplianceSettings.json", SearchOption.AllDirectories);

                if (foundFiles.Length > 0)
                {
                    string settingsPath = foundFiles[0];
                    complianceSettings = await compliance.LoadSettingsAsync(settingsPath);
                }
                else
                {
                    Logger.Debug("ComplianceSettings.json not found.");
                }

                if (compliance.Check(complianceSettings, parsedBomData))
                {
                    PipelineArtifactUploader.PrintWarning(compliance.GetResults().ToString());
                }
            }
        }

    }
}