﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// Program class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static Stopwatch CreatorStopWatch { get; set; }
        private static bool m_Verbose = false;
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Program() { }

        static async Task Main(string[] args)
        {
            CreatorStopWatch = new Stopwatch();
            CreatorStopWatch.Start();

            if (!m_Verbose && CommonHelper.IsAzureDevOpsDebugEnabled())
                m_Verbose = true;

            ISettingsManager settingsManager = new SettingsManager();
            CommonAppSettings appSettings = settingsManager.ReadConfiguration<CommonAppSettings>(args, FileConstant.appSettingFileName);
            ISW360ApicommunicationFacade sW360ApicommunicationFacade;
            ISw360ProjectService sw360ProjectService= Getsw360ProjectServiceObject(appSettings, out sW360ApicommunicationFacade);
            ProjectReleases projectReleases = new ProjectReleases();
            // do not change the order of getting ca tool information
            CatoolInfo caToolInformation = GetCatoolVersionFromProjectfile();
            Log4Net.CatoolCurrentDirectory = Directory.GetParent(caToolInformation.CatoolRunningLocation).FullName;


            string FolderPath = InitiateLogger(appSettings);
            settingsManager.CheckRequiredArgsToRun(appSettings, "Creator");
            int isValid = await CreatorValidator.ValidateAppSettings(appSettings, sw360ProjectService, projectReleases);
            if (isValid == -1)
            {
                CommonHelper.CallEnvironmentExit(-1);
            }

            Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Package creator >>>>>====================", null);
            Logger.Logger.Log(null, Level.Notice, $"\nStart of Package creator execution : {DateTime.Now}", null);

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Alert, $"Package creator is running in TEST mode \n", null);

            Logger.Logger.Log(null, Level.Notice, $"Input parameters used in Package Creator:\n\t" +
              $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
              $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
              $"BomFilePath\t\t --> {appSettings.BomFilePath}\n\t" +
              $"SW360Url\t\t --> {appSettings.SW360URL}\n\t" +
              $"SW360AuthTokenType\t --> {appSettings.SW360AuthTokenType}\n\t" +
              $"SW360ProjectName\t --> {appSettings.SW360ProjectName}\n\t" +
              $"SW360ProjectID\t\t --> {appSettings.SW360ProjectID}\n\t" +
              $"EnableFossTrigger\t --> {appSettings.EnableFossTrigger}\n\t" +
              $"RemoveDevDependency\t --> {appSettings.RemoveDevDependency}\n\t" +
              $"LogFolderPath\t\t --> {Path.GetFullPath(FolderPath)}\n\t", null);

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Notice, $"\tMode\t\t\t --> {appSettings.Mode}\n", null);

            await InitiatePackageCreatorProcess(appSettings, sw360ProjectService, sW360ApicommunicationFacade);

            Logger.Logger.Log(null, Level.Notice, $"End of Package Creator execution: {DateTime.Now}\n", null);
            
            // publish logs and bom file to pipeline artifact
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

        private static ISw360ProjectService Getsw360ProjectServiceObject(CommonAppSettings appSettings, out ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ISw360ProjectService sw360ProjectService;
            SW360ConnectionSettings sw360ConnectionSettings = new SW360ConnectionSettings()
            {
                SW360URL = appSettings.SW360URL,
                SW360AuthTokenType = appSettings.SW360AuthTokenType,
                Sw360Token = appSettings.Sw360Token,
                IsTestMode = appSettings.IsTestMode,
                Timeout = appSettings.TimeOut
            };


            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(sw360ConnectionSettings);
            sw360ProjectService = new Sw360ProjectService(sW360ApicommunicationFacade);
            return sw360ProjectService;
        }

        private static async Task InitiatePackageCreatorProcess(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService, ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ISW360CommonService sw360CommonService = new SW360CommonService(sW360ApicommunicationFacade);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sW360ApicommunicationFacade, sw360CommonService);
            ISW360Service sw360Service = new Sw360Service(sW360ApicommunicationFacade, sw360CommonService);
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
            IComponentCreator componentCreator = new ComponentCreator();
            List<ComparisonBomData> parsedBomData = await componentCreator.CycloneDxBomParser(appSettings, sw360Service, cycloneDXBomParser, creatorHelper);

            // initializing Component creation 
            await componentCreator.CreateComponentInSw360(appSettings, sw360CreatorService, sw360Service,
                 sw360ProjectService, new FileOperations(), creatorHelper, parsedBomData);
        }

        private static string InitiateLogger(CommonAppSettings appSettings)
        {
            string FolderPath;
            if (!string.IsNullOrEmpty(appSettings.LogFolderPath))
            {
                FolderPath = appSettings.LogFolderPath;
                Log4Net.Init(FileConstant.ComponentCreatorLog, appSettings.LogFolderPath, m_Verbose);
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
                Log4Net.Init(FileConstant.ComponentCreatorLog, FolderPath, m_Verbose);
            }

            return FolderPath;
        }
    }
}
