// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Responsible for building compositions and adding them to the BOM (Bill of Materials).
    /// </summary>
    public class CompositionBuilder : ICompositionBuilder
    {
        private readonly ComponentConfig _config;
        private readonly string _basePurl;
        private RuntimeInfo _runtimeInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositionBuilder"/> class.
        /// </summary>
        /// <param name="config">Optional configuration for component settings.</param>
        public CompositionBuilder(ComponentConfig config = null)
        {
            _config = config ?? new ComponentConfig();
            _basePurl = Dataconstant.PurlCheck()["NUGET"];
        }

        /// <summary>
        /// Adds compositions to the provided BOM based on the framework packages.
        /// </summary>
        /// <param name="bom">The BOM to which compositions will be added.</param>
        /// <param name="frameworkPackages">Framework packages grouped by framework moniker.</param>
        public void AddCompositionsToBom(Bom bom, Dictionary<string, Dictionary<string, NuGetVersion>> frameworkPackages, RuntimeInfo runtimeInfo)
        {
            _runtimeInfo = runtimeInfo;
            if (bom != null && frameworkPackages != null)
            {
                bom.Compositions = frameworkPackages.Select(CreateComposition).ToList();
            }
        }

        /// <summary>
        /// Creates a composition for a specific framework.
        /// </summary>
        /// <param name="framework">The framework and its associated packages.</param>
        /// <returns>A new <see cref="Composition"/> instance.</returns>
        private Composition CreateComposition(KeyValuePair<string, Dictionary<string, NuGetVersion>> framework)
        {
            return new Composition
            {
                Aggregate = Composition.AggregateType.Complete,
                Assemblies = new List<string> { CreateRuntimeComponentIdentifier(framework.Key) },
                Dependencies = CreateDependencyIdentifiers(framework.Value)
            };
        }

        /// <summary>
        /// Creates a runtime component identifier for a given framework moniker.
        /// </summary>
        /// <param name="frameworkMoniker">The framework moniker (e.g., "net6.0").</param>
        /// <returns>A runtime component identifier string.</returns>
        private string CreateRuntimeComponentIdentifier(string frameworkMoniker)
        {
            if (_runtimeInfo?.FrameworkReferences != null && _runtimeInfo.FrameworkReferences.Any(fr => fr.TargetFramework.Equals(frameworkMoniker, StringComparison.OrdinalIgnoreCase)))
            {
                var frameworkRef = _runtimeInfo.FrameworkReferences.First(fr => fr.TargetFramework.Equals(frameworkMoniker, StringComparison.OrdinalIgnoreCase));
                return $"{_basePurl}/{_config.RuntimePackage}@{frameworkRef.TargetingPackVersion}";
            }
            var version = ExtractFrameworkVersion(frameworkMoniker);
            return $"{_basePurl}/{_config.RuntimePackage}@{version}";
        }

        /// <summary>
        /// Creates a list of dependency identifiers for the given packages.
        /// </summary>
        /// <param name="dependencies">A dictionary of package names and their versions.</param>
        /// <returns>A list of dependency identifier strings.</returns>
        private List<string> CreateDependencyIdentifiers(Dictionary<string, NuGetVersion> dependencies)
        {
            return dependencies.Select(pkg =>
                $"{_basePurl}/{pkg.Key}@{pkg.Value.ToNormalizedString()}"
            ).ToList();
        }

        /// <summary>
        /// Extracts the framework version from a framework moniker.
        /// </summary>
        /// <param name="frameworkMoniker">The framework moniker (e.g., "net6.0").</param>
        /// <returns>The extracted version string.</returns>
        private string ExtractFrameworkVersion(string frameworkMoniker)
        {
            if (string.IsNullOrEmpty(frameworkMoniker))
                return _config.DefaultVersion;

            // Remove any platform-specific suffix (e.g., "net6.0-windows").
            if (frameworkMoniker.Contains('-'))
            {
                frameworkMoniker = frameworkMoniker.Split('-')[0];
            }

            // Ensure the moniker starts with "net".
            if (!frameworkMoniker.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                return _config.DefaultVersion;

            // Extract and normalize the version.
            var version = frameworkMoniker[3..];
            var parts = version.Split('.');

            return parts.Length switch
            {
                1 => $"{version}.0.0",
                2 => $"{version}.0",
                _ => version
            };
        }
    }

    /// <summary>
    /// Configuration settings for the <see cref="CompositionBuilder"/>.
    /// </summary>
    public class ComponentConfig
    {
        public string RuntimeName { get; set; } = ".NET Runtime";
        public string RuntimePackage { get; set; } = "dotnet-runtime";
        public string DefaultVersion { get; set; } = "0.0.0";
    }
}