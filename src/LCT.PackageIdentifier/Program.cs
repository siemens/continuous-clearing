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

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Program class
    /// </summary>
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

            string FolderPath = LogFolderInitialisation(appSettings);

            Logger.Logger.Log(null, Level.Notice, $"\n====================<<<<< Package Identifier >>>>>====================", null);
            Logger.Logger.Log(null, Level.Notice, $"\nStart of Package Identifier execution: {DateTime.Now}", null);

            if (appSettings.ProjectType.ToUpperInvariant() == "ALPINE")
            {
                Logger.Error($"\nPlease note that the Alpine feature is currently in preview state. This means it's available for testing and evaluation purposes. While functional, it may not yet include all planned features and could encounter occasional issues. Your feedback during this preview phase is appreciated as we work towards its official release. Thank you for exploring Alpine with us.");
            }

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Alert, $"Package Identifier is running in TEST mode \n", null);

            // Validate application settings
            await ValidateAppsettingsFile(appSettings);

            Logger.Logger.Log(null, Level.Notice, $"Input Parameters used in Package Identifier:\n\t" +
                $"PackageFilePath\t\t --> {appSettings.PackageFilePath}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.BomFolderPath}\n\t" +
                $"SBOMTemplateFilePath\t --> {appSettings.CycloneDxSBomTemplatePath}\n\t" +
                $"SW360Url\t\t --> {appSettings.SW360URL}\n\t" +
                $"SW360AuthTokenType\t --> {appSettings.SW360AuthTokenType}\n\t" +
                $"SW360ProjectName\t --> {appSettings.SW360ProjectName}\n\t" +
                $"SW360ProjectID\t\t --> {appSettings.SW360ProjectID}\n\t" +
                $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                $"LogFolderPath\t\t --> {Path.GetFullPath(FolderPath)}", null);

            if (appSettings.IsTestMode)
                Logger.Logger.Log(null, Level.Notice, $"\tMode\t\t\t --> {appSettings.Mode}\n", null);


            IBomCreator bomCreator = new BomCreator();
            bomCreator.JFrogService = GetJfrogService(appSettings);
            bomCreator.BomHelper = new BomHelper();

            //Validating JFrog Settings
            if (await bomCreator.CheckJFrogConnection())
            {
                await bomCreator.GenerateBom(appSettings, new BomHelper(), new FileOperations());
                Logger.Logger.Log(null, Level.Notice, $"End of Package Identifier execution : {DateTime.Now}\n", null);
            }
            else
            {
                Logger.Logger.Log(null, Level.Alert, $"JFrog Connection was not successfull!! \n", null);
            }
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

        private static async Task ValidateAppsettingsFile(CommonAppSettings appSettings)
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
            await BomValidator.ValidateAppSettings(appSettings, sw360ProjectService);
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
