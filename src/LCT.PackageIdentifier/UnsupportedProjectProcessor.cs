using LCT.Common.Interface;
using LCT.Common;
using LCT.PackageIdentifier.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using CycloneDX.Models;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using LCT.Common.Constants;

namespace LCT.PackageIdentifier
{
    public class UnsupportedProjectProcessor(ISpdxBomParser spdxBomParser) : IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        public Bom ParsePackageFile(CommonAppSettings appSettings)
        {
            List<string> configFiles;
            List<AlpinePackage> listofComponents = new List<AlpinePackage>();
            Bom bom = new Bom();
            List<Dependency> dependenciesForBOM = new();

            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Alpine);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                }
                else
                {
                    Logger.Debug($"ParsePackageFile():FileName: " + filepath);
                    listofComponents.AddRange(ParseCycloneDX(filepath, dependenciesForBOM));
                }

            }

            int initialCount = listofComponents.Count;
            //GetDistinctComponentList(ref listofComponents);
            //List<Component> listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            //BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;

            //bom.Components = listComponentForBOM;
            bom.Dependencies = dependenciesForBOM;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);

            //SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            //bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            return bom;
        }
        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData,
           CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            await Task.Yield();
            return componentData;
        }
        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings,
                                                          IJFrogService jFrogService,
                                                          IBomHelper bomhelper)
        {
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                CycloneBomProcessor.SetProperties(appSettings, component, ref modifiedBOM);
            }
            await Task.Yield();
            return modifiedBOM;
        }
        public List<AlpinePackage> ParseCycloneDX(string filePath, List<Dependency> dependenciesForBOM)
        {
            List<AlpinePackage> alpinePackages = new List<AlpinePackage>();
            ExtractDetailsForJson(filePath, ref alpinePackages, dependenciesForBOM);
            return alpinePackages;
        }

        private void ExtractDetailsForJson(string filePath, ref List<AlpinePackage> alpinePackages, List<Dependency> dependenciesForBOM)
        {
            Bom bom=new();
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {                
                bom = spdxBomParser.ParseSPDXBom(filePath);
            }
            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                AlpinePackage package = new AlpinePackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                };
                //SetSpdxComponentDetails(filePath, package);

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["ALPINE"]))
                {

                    alpinePackages.Add(package);
                    Logger.Debug($"ExtractDetailsForJson():ValidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"ExtractDetailsForJson():InvalidComponent : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
            }
            if (bom.Dependencies != null)
            {
                dependenciesForBOM.AddRange(bom.Dependencies);
            }
        }
    }
}
