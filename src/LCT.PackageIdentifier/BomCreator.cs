// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Logging;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dependency = CycloneDX.Models.Dependency;
using Directory = System.IO.Directory;
using Level = log4net.Core.Level;
using Metadata = CycloneDX.Models.Metadata;


namespace LCT.PackageIdentifier

{
    /// <summary>
    /// BomCreator model
    /// </summary>
    public class BomCreator : IBomCreator
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public readonly static BomKpiData bomKpiData = new();
        ComponentIdentification componentData;
        private readonly ICycloneDXBomParser CycloneDXBomParser;
        private readonly ISpdxBomParser SpdxBomParser;
        public IJFrogService JFrogService { get; set; }
        public IBomHelper BomHelper { get; set; }

        private readonly IFrameworkPackages _frameworkPackages;
        private readonly ICompositionBuilder _compositionBuilder;

        public static Jfrog jfrog { get; set; } = new Jfrog();
        public static SW360 sw360 { get; set; } = new SW360();
        public BomCreator(ICycloneDXBomParser cycloneDXBomParser, IFrameworkPackages frameworkPackages, ICompositionBuilder compositionBuilder,ISpdxBomParser spdxBomParser)
        {
            CycloneDXBomParser = cycloneDXBomParser;
            _frameworkPackages = frameworkPackages;
            _compositionBuilder = compositionBuilder;
            SpdxBomParser = spdxBomParser;
        }

        public async Task GenerateBom(CommonAppSettings appSettings,
                                      IBomHelper bomHelper,
                                      IFileOperations fileOperations,
                                      ProjectReleases projectReleases,
                                       CatoolInfo caToolInformation)
        {
            Logger.Debug($"GenerateBom():Start");
            Bom listOfComponentsToBom;
            jfrog = appSettings.Jfrog;
            sw360 = appSettings.SW360;
            // Calls package parser
            listOfComponentsToBom = await CallPackageParser(appSettings);
            Logger.Logger.Log(null, Level.Notice, $"No of components added to BOM after removing bundled & excluded components " +
                $"= {listOfComponentsToBom.Components.Count}", null);

            bomKpiData.ComponentsInComparisonBOM = listOfComponentsToBom.Components.Count;
            //Get project details for metadata properties

            //Add composition information to the BOM , if empty then add empty composition
            if (listOfComponentsToBom.Compositions == null || listOfComponentsToBom.Compositions.Count == 0)
            {
                listOfComponentsToBom.Compositions = [];
            }

            //sets metadata properties
            listOfComponentsToBom = CycloneBomProcessor.SetMetadataInComparisonBOM(listOfComponentsToBom,
                                                                                   appSettings,
                                                                                   projectReleases,
                                                                                   caToolInformation);

            string defaultProjectName = CommonIdentiferHelper.GetDefaultProjectName(appSettings);
            // Writes Comparison Bom
            Logger.Logger.Log(null, Level.Notice, $"Writing CycloneDX BOM..", null);
            WritecontentsToBOM(appSettings, bomKpiData, listOfComponentsToBom, defaultProjectName);
            Logger.Logger.Log(null, Level.Notice, $"Writing CycloneDX BOM completed", null);

            // Log warnings based on appSettings
            DisplayInformation.LogBomGenerationWarnings(appSettings);

            // Writes Kpi data 
            Program.BomStopWatch?.Stop();
            bomKpiData.TimeTakenByBomCreator = Program.BomStopWatch == null ? 0 :
              TimeSpan.FromMilliseconds(Program.BomStopWatch.ElapsedMilliseconds).TotalSeconds;
            fileOperations.WriteContentToFile(bomKpiData, appSettings.Directory.OutputFolder,
                FileConstant.BomKpiDataFileName, defaultProjectName);
            if (appSettings.SW360 != null)
            {
                // Writes Project Summary Url on CLI
                string projectURL = bomHelper.GetProjectSummaryLink(appSettings.SW360.ProjectID, appSettings.SW360.URL);
                bomKpiData.ProjectSummaryLink = $"Link to the summary page of the configured project:{appSettings.SW360.ProjectName} => {projectURL}\n";
            }

            // Writes kpi info to console table
            bomKpiData.InternalComponents = componentData.internalComponents != null ? componentData.internalComponents.Count : 0;
            bomHelper.WriteBomKpiDataToConsole(bomKpiData);

            if (appSettings.Jfrog != null)
            {
                LoggerHelper.WriteInternalComponentsTableInCli(componentData.internalComponents);
            }

            Logger.Debug($"GenerateBom():End");
        }

        private static void WritecontentsToBOM(CommonAppSettings appSettings, BomKpiData bomKpiData, Bom listOfComponentsToBom, string defaultProjectName)
        {
            WriteContentToCycloneDxBOM(appSettings, listOfComponentsToBom, ref bomKpiData, defaultProjectName);
        }

        private static void WriteContentToCycloneDxBOM(CommonAppSettings appSettings, Bom listOfComponentsToBom, ref BomKpiData bomKpiData, string defaultProjectName)
        {
            FileOperations fileOperations = new FileOperations();
            string bomFileName = CommonIdentiferHelper.GetBomFileName(appSettings);

            string outputFolderPath = appSettings.Directory.OutputFolder;
            string[] files = Directory.GetFiles(outputFolderPath);

            bool fileExists = files.Length > 0 && files.Any(file => Path.GetFileName(file).Equals(bomFileName, StringComparison.OrdinalIgnoreCase));
            if (fileExists && appSettings.MultipleProjectType)
            {
                string existingFilePath = files.FirstOrDefault(file => Path.GetFileName(file).Equals(bomFileName, StringComparison.OrdinalIgnoreCase));
                listOfComponentsToBom = fileOperations.CombineComponentsFromExistingBOM(listOfComponentsToBom, existingFilePath);
                bomKpiData.ComponentsInComparisonBOM = listOfComponentsToBom.Components.Count;
                string formattedString = CommonHelper.AddSpecificValuesToBOMFormat(listOfComponentsToBom);
                fileOperations.WriteContentToOutputBomFile(formattedString, outputFolderPath, FileConstant.BomFileName, defaultProjectName);
            }
            else
            {
                string formattedString = CommonHelper.AddSpecificValuesToBOMFormat(listOfComponentsToBom);
                fileOperations.WriteContentToOutputBomFile(formattedString, outputFolderPath, FileConstant.BomFileName, defaultProjectName);
            }

        }

        private async Task<Bom> CallPackageParser(CommonAppSettings appSettings)
        {
            IParser parser;

            switch (appSettings.ProjectType.ToUpperInvariant())
            {
                case "NPM":
                    parser = new NpmProcessor(CycloneDXBomParser, SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                case "NUGET":
                    parser = new NugetProcessor(CycloneDXBomParser, _frameworkPackages, _compositionBuilder,SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                case "MAVEN":
                    parser = new MavenProcessor(CycloneDXBomParser, SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                case "DEBIAN":
                    parser = new DebianProcessor(CycloneDXBomParser, SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                case "ALPINE":
                    parser = new AlpineProcessor(CycloneDXBomParser, SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                case "POETRY":
                    parser = new PythonProcessor(CycloneDXBomParser, SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                case "CONAN":
                    parser = new ConanProcessor(CycloneDXBomParser, SpdxBomParser);
                    return await ComponentIdentification(appSettings, parser);
                default:
                    Logger.Error($"GenerateBom():Invalid ProjectType - {appSettings.ProjectType}");
                    break;
            }
            return new Bom();
        }

        private async Task<Bom> ComponentIdentification(CommonAppSettings appSettings, IParser parser)
        {
            ComponentIdentification lstOfComponents;
            List<Component> components;
            Metadata metadata;
            Bom bom = new Bom();
            Bom unSupportedBomList = new Bom { Components = new List<Component>(),Dependencies = new List<Dependency>() };
            try
            {
                //Parsing the input file
                bom = parser.ParsePackageFile(appSettings,ref unSupportedBomList);
                metadata = bom.Metadata;
                componentData = new ComponentIdentification()
                {
                    comparisonBOMData = bom.Components,
                    internalComponents = new List<Component>()
                };

                if (appSettings.Jfrog != null)
                {
                    //Identification of internal components
                    Logger.Logger.Log(null, Level.Notice, $"Identifying the internal components", null);
                    lstOfComponents = await parser.IdentificationOfInternalComponents(componentData, appSettings, JFrogService, BomHelper);
                    components = lstOfComponents.comparisonBOMData;
                    //Setting the artifactory repo info
                    components = await parser.GetJfrogRepoDetailsOfAComponent(components, appSettings, JFrogService, BomHelper);
                    bom.Components = components;
                }
                else
                {
                    Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
                    foreach (var component in bom.Components)
                    {
                        bool propertyExists = component.Properties.Any(p => p.Name == Dataconstant.Cdx_ProjectType);
                        if (!propertyExists)
                        {
                            component.Properties.Add(projectType);
                        }

                    }
                }
                bom.Metadata = metadata;
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"ComponentIdentification: {ex}");
            }
            bomKpiData.UnsupportedComponentsFromSpdxFile = unSupportedBomList.Components.Count;
            bom.Components.AddRange(unSupportedBomList.Components);
            bom.Dependencies.AddRange(unSupportedBomList.Dependencies);
            return bom;
        }       
        public async Task<bool> CheckJFrogConnection(CommonAppSettings appSettings)
        {
            if (appSettings.Jfrog != null)
            {
                var response = await JFrogService.CheckJFrogConnectivity();
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        LoggerHelper.JfrogConnectionInfoDisplayForCli();
                        return true;
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Logger.Error($"Check the JFrog token validity/permission..");
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logger.Error($"Check the provided JFrog server details..");
                    }
                    else
                    {
                        Logger.Error($"JFrog Connection was not successfull check the server status.");
                    }
                }
                return false;
            }
            return true;

        }        
    }
}
