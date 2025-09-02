// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Interface;
using LCT.PackageIdentifier.Model;
using log4net;
using System.Collections.Generic;
using System.Reflection;
using Dependency = CycloneDX.Models.Dependency;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// The ChocoProcessor class for parsing choco.config files
    /// </summary>
    public class ChocoProcessor(ICycloneDXBomParser cycloneDXBomParser,
                         ISpdxBomParser spdxBomParser) : NugetProcessor(cycloneDXBomParser, null, null, spdxBomParser)
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };

        public override Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Choco);
            List<Component> chocoComponents = new();
            List<NugetPackage> nugetPackages = new();
            Bom bom = new();

            foreach (string filepath in configFiles)
            {
                Logger.Debug($"ParsePackageFile():FileName: {filepath}");
                var chocoList = ParsePackageConfig(filepath, appSettings);
                nugetPackages.AddRange(chocoList);
            }

            //Dependencies are not extracted for choco as of now
            ConvertToCycloneDXModel(chocoComponents, nugetPackages, null);

            bom.Components = chocoComponents;
            // No dependencies for now
            bom.Dependencies = new List<Dependency>();

            if (bom != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);

            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            return bom;
        }
    }
}
