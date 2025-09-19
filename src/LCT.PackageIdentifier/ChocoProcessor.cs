// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
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

        /// <summary>
        /// Parses package configuration files from the specified input folder and generates a Bill of Materials (BOM).
        /// </summary>
        /// <remarks>This method scans the input folder specified in <paramref name="appSettings"/> for
        /// package configuration files, processes them to extract components and dependencies, and converts the data
        /// into a CycloneDX-compatible BOM format. Currently, dependencies for Chocolatey packages are not
        /// extracted.</remarks>
        /// <param name="appSettings">The application settings containing the input folder path and other configuration options.</param>
        /// <param name="unSupportedBomList">A reference to a BOM object that will be populated with unsupported components and dependencies encountered
        /// during parsing.</param>
        /// <returns>A <see cref="Bom"/> object containing the parsed components and dependencies from the package configuration
        /// files.</returns>
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
            AddSiemensDirectProperty(ref bom);
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);

            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            return bom;
        }

        /// <summary>
        /// Adds the SiemensDirect property to each component in the specified BOM (Bill of Materials).
        /// </summary>
        /// <remarks>This method iterates through all components in the provided BOM and ensures that each
        /// component has a SiemensDirect property set to <see langword="true"/>. If the component's properties
        /// collection is null, it initializes the collection before adding the property. Duplicate properties with the
        /// same key are removed before adding the new property.</remarks>
        /// <param name="bom">A reference to the <see cref="Bom"/> object whose components will be updated with the SiemensDirect
        /// property.</param>
        public override void AddSiemensDirectProperty(ref Bom bom)
        {
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                // setting SiemensDirect property to true by default for choco packages
                const string siemensDirectValue = "true";

                component.Properties ??= new List<Property>();
                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_SiemensDirect,
                    siemensDirectValue);
                component.Properties = properties;
            }

            bom.Components = bomComponentsList;
        }
    }
}
