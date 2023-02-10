// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.Common;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using log4net;
using log4net.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

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
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.ComponentsinPackageLockJsonFile)),bomKpiData.ComponentsinPackageLockJsonFile },
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.DevDependentComponents)),bomKpiData.DevDependentComponents},
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.BundledComponents)),bomKpiData.BundledComponents},
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.ComponentsExcluded)),bomKpiData.ComponentsExcluded},
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.DuplicateComponents)),bomKpiData.DuplicateComponents},
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.InternalComponents)),bomKpiData.InternalComponents},
                {CommonHelper.Convert(bomKpiData,nameof(bomKpiData.ComponentsInComparisonBOM)),bomKpiData.ComponentsInComparisonBOM }
            };

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
        #endregion
    }
}