// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// BomCreator model
    /// </summary>
    public class BomCreator : IBomCreator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly BomKpiData bomKpiData = new();
        ComponentIdentification componentData;

        public IJFrogService JFrogService { get; set; }
        public IBomHelper BomHelper { get; set; }

        public async Task GenerateBom(CommonAppSettings appSettings, IBomHelper bomHelper, IFileOperations fileOperations)
        {
            Logger.Debug($"GenerateBom():Start");
            Bom listOfComponentsToBom;

            // Calls package parser
            listOfComponentsToBom = await CallPackageParser(appSettings);
            Logger.Logger.Log(null, Level.Notice, $"No of components to BOM after removing bundled, dev dependency & " +
                $"internal components = {listOfComponentsToBom.Components.Count}", null);


            bomKpiData.ComponentsInComparisonBOM = listOfComponentsToBom.Components.Count;


            //sets metadata properties
            listOfComponentsToBom = CycloneBomProcessor.SetMetadataInComparisonBOM(listOfComponentsToBom, appSettings);


            // Writes Comparison Bom
            Logger.Logger.Log(null, Level.Notice, $"Writing CycloneDX BOM..", null);
            WritecontentsToBOM(appSettings, bomKpiData, listOfComponentsToBom);
            Logger.Logger.Log(null, Level.Notice, $"Writing CycloneDX BOM completed", null);


            // Writes Kpi data 
            Program.BomStopWatch?.Stop();
            bomKpiData.TimeTakenByBomCreator = Program.BomStopWatch == null ? 0 :
              TimeSpan.FromMilliseconds(Program.BomStopWatch.ElapsedMilliseconds).TotalSeconds;
            fileOperations.WriteContentToFile(bomKpiData, appSettings.BomFolderPath,
                FileConstant.BomKpiDataFileName, appSettings.SW360ProjectName);

            // Writes Project Summary Url on CLI
            string projectURL = bomHelper.GetProjectSummaryLink(appSettings.SW360ProjectID, appSettings.SW360URL);
            bomKpiData.ProjectSummaryLink = $"Link to the summary page of the configurred project:{appSettings.SW360ProjectName} => {projectURL}\n";

            // Writes kpi info to console table
            bomKpiData.InternalComponents = componentData.internalComponents != null ? componentData.internalComponents.Count : 0;
            bomHelper.WriteBomKpiDataToConsole(bomKpiData);

            //Writes internal component ist to kpi

            bomHelper.WriteInternalComponentsListToKpi(componentData.internalComponents);
            Logger.Debug($"GenerateBom():End");
        }

        private static void WritecontentsToBOM(CommonAppSettings appSettings, BomKpiData bomKpiData, Bom listOfComponentsToBom)
        {
           
                WriteContentToComparisonBOM(appSettings, listOfComponentsToBom, ref bomKpiData);
       

        }



        private static void WriteContentToComparisonBOM(CommonAppSettings appSettings, Bom listOfComponentsToBom, ref BomKpiData bomKpiData)
        {
            IFileOperations fileOperations = new FileOperations();
            if (string.IsNullOrEmpty(appSettings.IdentifierBomFilePath))
            {
                fileOperations.WriteContentToFile(listOfComponentsToBom, appSettings.BomFolderPath,
                FileConstant.BomFileName, appSettings.SW360ProjectName);
            }
            else
            {
                listOfComponentsToBom = fileOperations.CombineComponentsFromExistingBOM(listOfComponentsToBom, appSettings.IdentifierBomFilePath);
                bomKpiData.ComponentsInComparisonBOM = listOfComponentsToBom.Components.Count;
                fileOperations.WriteContentToFile(listOfComponentsToBom, appSettings.BomFolderPath,
                  FileConstant.BomFileName, appSettings.SW360ProjectName);
            }
        }

        private async Task<Bom> CallPackageParser(CommonAppSettings appSettings)
        {
            IParser parser;


            switch (appSettings.ProjectType.ToUpperInvariant())
            {
                case "NPM":
                    parser = new NpmProcessor();
                    return await ComponentIdentification(appSettings, parser);
                case "NUGET":
                    parser = new NugetProcessor();
                    return await ComponentIdentification(appSettings, parser);
                case "MAVEN":
                    parser = new MavenProcessor();
                    return await ComponentIdentification(appSettings, parser);
                case "DEBIAN":
                    parser = new DebianProcessor();
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
            try
            {
                //Parsing the input file
                bom = parser.ParsePackageFile(appSettings);
                metadata = bom.Metadata;
                componentData = new ComponentIdentification()
                {
                    comparisonBOMData = bom.Components,
                    internalComponents = new List<Component>()
                };

                //Identification of internal components
                Logger.Logger.Log(null, Level.Notice, $"Identifying the internal components", null);
                lstOfComponents = await parser.IdentificationOfInternalComponents(componentData, appSettings, JFrogService, BomHelper);
                components = lstOfComponents.comparisonBOMData;

                //Setting the artifactory repo info
                components = await parser.GetJfrogRepoDetailsOfAComponent(components, appSettings, JFrogService, BomHelper);
                bom.Components = components;
                bom.Metadata = metadata;
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"ComponentIdentification: {ex}");
            }
            return bom;
        }
    }
}
