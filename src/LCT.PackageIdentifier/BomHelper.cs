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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Level = log4net.Core.Level;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// BomHelper class
    /// </summary>
    public class BomHelper : IBomHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region public methods

        public string GetProjectSummaryLink(string projectId, string sw360Url)
        {
            Logger.Debug("starting method GetProjectSummaryLink");
            return $"{sw360Url}{ApiConstant.Sw360ProjectUrlApiSuffix}{projectId}";
        }

        public void WriteBomKpiDataToConsole(BomKpiData bomKpiData)
        {
            Dictionary<string, int> printList = new Dictionary<string, int>()
    {
        {CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsinPackageLockJsonFile)), bomKpiData.ComponentsinPackageLockJsonFile },
        {CommonHelper.Convert(bomKpiData, nameof(bomKpiData.DevDependentComponents)), bomKpiData.DevDependentComponents},
        {CommonHelper.Convert(bomKpiData, nameof(bomKpiData.BundledComponents)), bomKpiData.BundledComponents},
        {CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsExcluded)), bomKpiData.ComponentsExcluded},
        {CommonHelper.Convert(bomKpiData, nameof(bomKpiData.DuplicateComponents)), bomKpiData.DuplicateComponents}

    };
            if (BomCreator.sw360 != null)
            {
                printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsExcludedSW360)), bomKpiData.ComponentsExcludedSW360);
            }
            if (BomCreator.jfrog != null)
            {
                printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.InternalComponents)), bomKpiData.InternalComponents);
                printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ThirdPartyRepoComponents)), bomKpiData.ThirdPartyRepoComponents);
                printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.DevdependencyComponents)), bomKpiData.DevdependencyComponents);
                printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ReleaseRepoComponents)), bomKpiData.ReleaseRepoComponents);
                printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.UnofficialComponents)), bomKpiData.UnofficialComponents);
            }
            printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsinSBOMTemplateFile)), bomKpiData.ComponentsinSBOMTemplateFile);
            printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsUpdatedFromSBOMTemplateFile)), bomKpiData.ComponentsUpdatedFromSBOMTemplateFile);
            printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.UnsupportedComponentsFromSpdxFile)), bomKpiData.UnsupportedComponentsFromSpdxFile);
            printList.Add(CommonHelper.Convert(bomKpiData, nameof(bomKpiData.ComponentsInComparisonBOM)), bomKpiData.ComponentsInComparisonBOM);
            Dictionary<string, double> printTimingList = new Dictionary<string, double>()
            {
                { "PackageIdentifier",bomKpiData.TimeTakenByBomCreator }
            };

            CommonHelper.ProjectSummaryLink = bomKpiData.ProjectSummaryLink;
            CommonHelper.WriteToConsoleTable(printList, printTimingList);
        }
        public void WriteInternalComponentsListToKpi(List<Component> internalComponents)
        {
            const string Name = "Name";
            const string Version = "Version";

            if (internalComponents?.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* Internal Components Identified which will not be sent for clearing:", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,-45} {"|",5} {Version,35} {"|",10}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);

                foreach (var item in internalComponents)
                {
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,35} {"|",10}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 98)),5}", null);
                }
                Logger.Info("\n");
            }
        }
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

        private static void CheckFileExistence(string inputFolder, string[] relatedExtensions,
            Dictionary<string, string> foundFiles, List<string> missingFiles)
        {
            foreach (var related in relatedExtensions)
            {
                string relatedFile = Path.Combine(inputFolder, related);
                if (File.Exists(relatedFile))
                {
                    foundFiles[related] = relatedFile;
                }
                else
                {
                    missingFiles.Add(related);
                }
            }
        }

        private static void HandleMissingFiles(List<string> missingFiles, string filename)
        {
            foreach (var missingFile in missingFiles)
            {
                if (missingFile.EndsWith(".sig", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Error($"Naming Convention Error: The certificate file(s) for the SPDX document '{filename}' are missing. Please ensure that signature files are named in the format '{filename}.sig'.");
                }
                else if (missingFile.EndsWith(".pem", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Error($"Naming Convention Error: The certificate file(s) for the SPDX document '{filename}' are missing. Please ensure that .pem files are named in the format '{filename}.pem'.");
                }
            }
        }

        private static void ValidateFoundFiles(string filepath, string filename, Dictionary<string, string> foundFiles)
        {
            string sigFilePath = foundFiles.TryGetValue($"{filename}.sig", out string sigFile) ? sigFile : string.Empty;
            string pemFilePath = foundFiles.TryGetValue($"{filename}.pem", out string pemFile) ? pemFile : string.Empty;

            bool isValidFile = PemSignatureVerifier.ValidatePem(filepath, sigFilePath, pemFilePath);
            if (!isValidFile)
            {
                Logger.Warn($"The signature of the SPDX file '{filename}' is not valid. Please check the signature and certificate files.");
                Logger.Warn($"Currently processing the SPDX file '{filename}' without signature verification.");
            }
        }
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

        public static Bom ParseBomFile(string filePath, ISpdxBomParser spdxBomParser, ICycloneDXBomParser cycloneDXBomParser, CommonAppSettings appSettings, ref Bom listUnsupportedComponents)
        {
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                Bom bom;
                bom = spdxBomParser.ParseSPDXBom(filePath);
                SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
                SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filePath);
                return bom;
            }
            else
            {
                return cycloneDXBomParser.ParseCycloneDXBom(filePath);
            }
        }

        #endregion
    }
}