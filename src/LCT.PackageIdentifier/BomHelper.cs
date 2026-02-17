// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model.AQL;
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// BomHelper class
    /// </summary>
    public class BomHelper : IBomHelper
    {
        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Builds a URL that links to the project summary page in SW360.
        /// </summary>
        /// <param name="projectId">SW360 project identifier.</param>
        /// <param name="sw360Url">Base SW360 URL.</param>
        /// <returns>Full URL to the project's summary page.</returns>
        public string GetProjectSummaryLink(string projectId, string sw360Url)
        {
            Logger.Debug("starting method GetProjectSummaryLink");
            return $"{sw360Url}{ApiConstant.Sw360ProjectUrlApiSuffix}{projectId}";
        }

        /// <summary>
        /// Writes BOM KPI data to the console in a formatted table.
        /// </summary>
        /// <param name="bomKpiData">KPI data collected during BOM creation.</param>
        public void WriteBomKpiDataToConsole(BomKpiData bomKpiData)
        {
            KpiNames identifierKpiNames = IdentifyKpiNames(bomKpiData);
            Dictionary<string, int> printList = new Dictionary<string, int>()
    {
        {identifierKpiNames.ComponentsInInputFile, bomKpiData.ComponentsinPackageLockJsonFile },
        {identifierKpiNames.DevelopmentComponents, bomKpiData.DevDependentComponents},
        {identifierKpiNames.BundledComponents, bomKpiData.BundledComponents},
        {identifierKpiNames.InvalidComponentsExcluded, bomKpiData.ComponentsExcluded},
        {identifierKpiNames.DuplicateComponents, bomKpiData.DuplicateComponents}

    };
            if (BomCreator.sw360 != null)
            {
                printList.Add(identifierKpiNames.ManuallyExcludedSw360, bomKpiData.ComponentsExcludedSW360);
            }
            if (BomCreator.jfrog != null)
            {
                printList.Add(identifierKpiNames.InternalComponents, bomKpiData.InternalComponents);
                printList.Add(identifierKpiNames.PackagesPresentIn3rdPartyRepo, bomKpiData.ThirdPartyRepoComponents);
                printList.Add(identifierKpiNames.PackagesPresentInDevDepRepo, bomKpiData.DevdependencyComponents);
                printList.Add(identifierKpiNames.PackagesPresentInReleaseRepo, bomKpiData.ReleaseRepoComponents);
                printList.Add(identifierKpiNames.PackagesNotPresentInOfficialRepo, bomKpiData.UnofficialComponents);
            }
            printList.Add(identifierKpiNames.ComponentsAddedFromSBOMTemplate, bomKpiData.ComponentsinSBOMTemplateFile);
            printList.Add(identifierKpiNames.ComponentsOverWrittenFromSBOMTemplate, bomKpiData.ComponentsUpdatedFromSBOMTemplateFile);
            printList.Add(identifierKpiNames.ComponentsFromTheSPDXImportedAsBaselineEntries, bomKpiData.UnsupportedComponentsFromSpdxFile);
            printList.Add(identifierKpiNames.ComponentsInBOM, bomKpiData.ComponentsInComparisonBOM);
            Dictionary<string, double> printTimingList = new Dictionary<string, double>()
            {
                { "PackageIdentifier",bomKpiData.TimeTakenByBomCreator }
            };

            CommonHelper.ProjectSummaryLink = bomKpiData.ProjectSummaryLink;
            LoggerHelper.WriteToConsoleTable(printList, printTimingList, bomKpiData.ProjectSummaryLink, Dataconstant.Identifier, identifierKpiNames);
        }

        /// <summary>
        /// Validates related signature/certificate files for an SPDX file and logs naming issues.
        /// </summary>
        /// <param name="filepath">Path to the SPDX file.</param>
        /// <param name="appSettings">Application settings containing input folder info.</param>
        public static void NamingConventionOfSPDXFile(string filepath, CommonAppSettings appSettings)
        {
            string filename = Path.GetFileName(filepath);
            var relatedExtensions = new[] { $"{filename}.pem", $"{filename}.sig" };

            var foundFiles = new Dictionary<string, string>();
            var missingFiles = new List<string>();

            CheckFileExistence(appSettings.Directory.InputFolder, relatedExtensions, foundFiles, missingFiles);

            if (missingFiles.Count > 0)
            {
                HandleMissingFiles(missingFiles, filename);
            }
            else
            {
                ValidateFoundFiles(filepath, filename, foundFiles);
            }
        }

        /// <summary>
        /// Checks whether the related certificate/signature files exist in the input folder.
        /// </summary>
        /// <param name="inputFolder">Input folder path to search.</param>
        /// <param name="relatedExtensions">Array of related file names to check.</param>
        /// <param name="foundFiles">Dictionary to populate with found file paths keyed by name.</param>
        /// <param name="missingFiles">List to populate with missing file names.</param>
        private static void CheckFileExistence(string inputFolder, string[] relatedExtensions,
            Dictionary<string, string> foundFiles, List<string> missingFiles)
        {
            foreach (var related in relatedExtensions)
            {
                string relatedFile = Path.Combine(inputFolder, related);
                if (System.IO.File.Exists(relatedFile))
                {
                    foundFiles[related] = relatedFile;
                }
                else
                {
                    missingFiles.Add(related);
                }
            }
        }

        /// <summary>
        /// Logs errors for missing certificate/signature files using a helpful message.
        /// </summary>
        /// <param name="missingFiles">List of missing related files.</param>
        /// <param name="filename">Base SPDX filename.</param>
        private static void HandleMissingFiles(List<string> missingFiles, string filename)
        {
            foreach (var missingFile in missingFiles)
            {
                if (missingFile.EndsWith(".sig", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.ErrorFormat("Naming Convention Error: The certificate file(s) for the SPDX document '{0}' are missing. Please ensure that signature files are named in the format '{0}.sig'.", filename);
                }
                else if (missingFile.EndsWith(".pem", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.ErrorFormat("Naming Convention Error: The certificate file(s) for the SPDX document '{0}' are missing. Please ensure that .pem files are named in the format '{0}.pem'.", filename);
                }
            }
        }

        /// <summary>
        /// Validates the found signature and certificate files for the SPDX file and logs warnings on failure.
        /// </summary>
        /// <param name="filepath">Path to the SPDX file.</param>
        /// <param name="filename">SPDX file name.</param>
        /// <param name="foundFiles">Dictionary of discovered related files.</param>
        private static void ValidateFoundFiles(string filepath, string filename, Dictionary<string, string> foundFiles)
        {
            string sigFilePath = foundFiles.TryGetValue($"{filename}.sig", out string sigFile) ? sigFile : string.Empty;
            string pemFilePath = foundFiles.TryGetValue($"{filename}.pem", out string pemFile) ? pemFile : string.Empty;

            bool isValidFile = PemSignatureVerifier.ValidatePem(filepath, sigFilePath, pemFilePath);
            if (!isValidFile)
            {
                Logger.WarnFormat("The signature of the SPDX file '{0}' is not valid. Please check the signature and certificate files.", filename);
                Logger.WarnFormat("Currently processing the SPDX file '{0}' without signature verification.", filename);
            }
        }

        /// <summary>
        /// Executes 'npm view' to retrieve the shasum for the specified package version.
        /// </summary>
        /// <param name="name">Package name.</param>
        /// <param name="version">Package version.</param>
        /// <returns>SHA sum string or empty string if unavailable.</returns>
        public static string GetHashCodeUsingNpmView(string name, string version)
        {
            string hashCode;
            Process p = new Process();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                p.StartInfo.FileName = Path.Combine(@"/bin/bash");
                p.StartInfo.Arguments = $"-c \" npm view {name}@{version} dist.shasum \"";
                Logger.Debug($"GetHashCodeUsingNpmView():Linux OS Found!!");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                p.StartInfo.Arguments = $"/c npm view {name}@{version}  dist.shasum";
                Logger.Debug($"GetHashCodeUsingNpmView():Windows OS Found!!");
            }
            else
            {
                Logger.Debug($"GetHashCodeUsingNpmView():OS Details not Found!!");
            }

            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo);
            Result result = processResult?.Result;

            hashCode = result?.StdOut;
            return hashCode?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Executes Maven dependency:list to generate a dependency output file. (Obsolete)
        /// </summary>
        /// <param name="bomFilePath">Path to the POM or BOM file.</param>
        /// <param name="depFilePath">Output dependency file path.</param>
        /// <returns>Result containing process stdout/stderr.</returns>
        [Obsolete("not used")]
        [ExcludeFromCodeCoverage]
        public static Result GetDependencyList(string bomFilePath, string depFilePath)
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Process p = new Process();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            if (isWindows)
            {
                p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                p.StartInfo.Arguments = $"/c mvn -f \"{bomFilePath}\" dependency:list -DoutputFile=\"{depFilePath}\" -DappendOutput=\"true\"";

            }
            else
            {
                p.StartInfo.FileName = Path.Combine(@"mvn");
                p.StartInfo.Arguments = $"-f {bomFilePath} dependency:list -DoutputFile={depFilePath} -DappendOutput=true";

            }
            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo);
            Result result = processResult?.Result;
            return result;


        }

        /// <summary>
        /// Builds a display name for a component including group if available.
        /// </summary>
        /// <param name="item">CycloneDX component instance.</param>
        /// <returns>Full name (group/name) or name if no group present.</returns>
        public string GetFullNameOfComponent(Component item)
        {
            if (!string.IsNullOrEmpty(item.Group))
            {
                return $"{item.Group}/{item.Name}";
            }
            else
            {
                return item.Name;
            }
        }

        /// <summary>
        /// Asynchronously retrieves AQL results for components from the specified repositories.
        /// </summary>
        /// <param name="repoList">Array of repository names to query.</param>
        /// <param name="jFrogService">JFrog service to execute queries.</param>
        /// <returns>Asynchronously returns a list of AQL results aggregated from all repos.</returns>
        public async Task<List<AqlResult>> GetListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            List<AqlResult> aqlResultList = new();
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var test = await jFrogService.GetInternalComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(test);
                }
            }

            return aqlResultList;
        }

        /// <summary>
        /// Asynchronously retrieves NPM-style component information from the specified repositories.
        /// </summary>
        /// <param name="repoList">Array of repository names.</param>
        /// <param name="jFrogService">JFrog service to execute queries.</param>
        /// <returns>Asynchronously returns aggregated AQL results for NPM components.</returns>
        public async Task<List<AqlResult>> GetNpmListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            List<AqlResult> aqlResultList = new();
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var componentRepoData = await jFrogService.GetNpmComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(componentRepoData);
                }
            }

            return aqlResultList;
        }

        /// <summary>
        /// Asynchronously retrieves Cargo-style component information from the specified repositories.
        /// </summary>
        /// <param name="repoList">Array of repository names.</param>
        /// <param name="jFrogService">JFrog service to execute queries.</param>
        /// <returns>Asynchronously returns aggregated AQL results for Cargo components.</returns>
        public async Task<List<AqlResult>> GetCargoListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            List<AqlResult> aqlResultList = new();
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var componentRepoData = await jFrogService.GetCargoComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(componentRepoData);
                }
            }

            return aqlResultList;
        }

        /// <summary>
        /// Asynchronously retrieves PyPI-style component information from the specified repositories.
        /// </summary>
        /// <param name="repoList">Array of repository names.</param>
        /// <param name="jFrogService">JFrog service to execute queries.</param>
        /// <returns>Asynchronously returns aggregated AQL results for PyPI components.</returns>
        public async Task<List<AqlResult>> GetPypiListOfComponentsFromRepo(string[] repoList, IJFrogService jFrogService)
        {
            List<AqlResult> aqlResultList = new();
            if (repoList != null && repoList.Length > 0)
            {
                foreach (var repo in repoList)
                {
                    var componentRepoData = await jFrogService.GetPypiComponentDataByRepo(repo) ?? new List<AqlResult>();
                    aqlResultList.AddRange(componentRepoData);
                }
            }

            return aqlResultList;
        }

        /// <summary>
        /// Parses a CycloneDX or SPDX BOM file and returns a Bom instance. Unsupported components are accumulated.
        /// </summary>
        /// <param name="filePath">Path to the BOM file.</param>
        /// <param name="spdxBomParser">SPDX parser instance.</param>
        /// <param name="cycloneDXBomParser">CycloneDX parser instance.</param>
        /// <param name="appSettings">Application settings used for SPDX validation.</param>
        /// <param name="listUnsupportedComponents">Reference list where unsupported components will be added.</param>
        /// <returns>Parsed Bom object.</returns>
        public static Bom ParseBomFile(string filePath, ISpdxBomParser spdxBomParser, ICycloneDXBomParser cycloneDXBomParser, CommonAppSettings appSettings, ref Bom listUnsupportedComponents)
        {
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                Logger.DebugFormat("ParseBomFile():Spdx file detected: {0}", filePath);
                Bom bom;
                bom = spdxBomParser.ParseSPDXBom(filePath);
                LogHandlingHelper.IdentifierInputFileComponents(filePath, bom.Components);
                SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
                SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filePath);
                return bom;
            }
            else
            {
                Logger.DebugFormat("ParseBomFile():CycloneDX file detected: {0}", filePath);
                Bom bom;
                bom = cycloneDXBomParser.ParseCycloneDXBom(filePath);
                LogHandlingHelper.IdentifierInputFileComponents(filePath, bom.Components);
                return bom;
            }
        }
        public static string GetReleaseExternalId(string name, string version, string purlBase)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{purlBase}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        /// <summary>
        /// Maps BomKpiData properties to human readable KPI names used for printing.
        /// </summary>
        /// <param name="bomKpiData">KPI data instance to map.</param>
        /// <returns>Structure with KPI display name mappings.</returns>
        private static KpiNames IdentifyKpiNames(BomKpiData bomKpiData)
        {
            KpiNames identifierKpiNames = new KpiNames();
            identifierKpiNames.ComponentsInInputFile = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsinPackageLockJsonFile));
            identifierKpiNames.DevelopmentComponents = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.DevDependentComponents));
            identifierKpiNames.BundledComponents = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.BundledComponents));
            identifierKpiNames.InvalidComponentsExcluded = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsExcluded));
            identifierKpiNames.DuplicateComponents = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.DuplicateComponents));
            identifierKpiNames.ManuallyExcludedSw360 = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsExcludedSW360));
            identifierKpiNames.InternalComponents = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.InternalComponents));
            identifierKpiNames.PackagesPresentIn3rdPartyRepo = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ThirdPartyRepoComponents));
            identifierKpiNames.PackagesPresentInDevDepRepo = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.DevdependencyComponents));
            identifierKpiNames.PackagesPresentInReleaseRepo = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ReleaseRepoComponents));
            identifierKpiNames.PackagesNotPresentInOfficialRepo = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.UnofficialComponents));
            identifierKpiNames.ComponentsAddedFromSBOMTemplate = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsinSBOMTemplateFile));
            identifierKpiNames.ComponentsOverWrittenFromSBOMTemplate = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsUpdatedFromSBOMTemplateFile));
            identifierKpiNames.ComponentsFromTheSPDXImportedAsBaselineEntries = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.UnsupportedComponentsFromSpdxFile));
            identifierKpiNames.ComponentsInBOM = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsInComparisonBOM));
            identifierKpiNames.ComponentsWithSourceURL = CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsWithSourceURL));

            return identifierKpiNames;
        }

        /// <summary>
        /// Filters and returns components that match the specified purl prefix for a project type.
        /// </summary>
        /// <param name="componentsForBOM">List of components to evaluate.</param>
        /// <param name="purlPrefix">PURL prefix to match (project type specific).</param>
        /// <param name="projectType">Project type label used in logging.</param>
        /// <returns>List of components that match the exclusion criteria.</returns>
        public static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM, string purlPrefix, string projectType)
        {
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) &&
                    !string.IsNullOrEmpty(componentsInfo.Version) &&
                    !string.IsNullOrEmpty(componentsInfo.Purl) &&
                    componentsInfo.Purl.Contains(purlPrefix))
                {
                    components.Add(componentsInfo);
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.DebugFormat("GetExcludedComponentsList():InvalidComponent For {0} : Component Details : {1} @ {2} @ {3}", projectType, componentsInfo.Name, componentsInfo.Version, componentsInfo.Purl);
                }
            }
            return components;
        }

        /// <summary>
        /// Removes duplicate components from the list based on name, version and PURL, updating KPI counts.
        /// </summary>
        /// <param name="listofComponents">Reference to the list of components to deduplicate.</param>
        public static void GetDistinctComponentList(ref List<Component> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.Purl }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;

        }

        /// <summary>
        /// Removes components excluded by settings from the provided BOM and updates KPI counters.
        /// </summary>
        /// <param name="appSettings">Application settings with exclude lists.</param>
        /// <param name="cycloneDXBOM">BOM to filter.</param>
        /// <returns>Filtered BOM.</returns>
        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM,
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        /// <summary>
        /// Adds standard properties for components that were manually added to the BOM.
        /// </summary>
        /// <param name="componentsForBOM">List of manually added components to update.</param>
        public static void GetDetailsforManuallyAddedComp(List<Component> componentsForBOM)
        {
            foreach (var component in componentsForBOM)
            {
                component.Properties ??= new List<Property>();
                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_IsDevelopment,
                    "false");
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_IdentifierType,
                    Dataconstant.ManullayAdded);
                component.Properties = properties;
            }
        }
        #endregion

        #region Events
        #endregion
    }
}