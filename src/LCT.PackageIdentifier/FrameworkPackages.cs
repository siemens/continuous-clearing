// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.PackageIdentifier.Interface;
using log4net;
using log4net.Core;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    public class FrameworkPackages : IFrameworkPackages
    {
        #region Fields
        readonly Dictionary<string, Dictionary<string, NuGetVersion>> _foundFrameworkPackages = new Dictionary<string, Dictionary<string, NuGetVersion>>();
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Discovers framework packages for the provided list of NuGet lock file paths.
        /// </summary>
        /// <param name="lockFilePaths">List of paths to project.assets.json (lock) files to analyze.</param>
        /// <returns>
        /// A dictionary keyed by framework identifier containing package name -> <see cref="NuGetVersion"/> mappings.
        /// </returns>
        public Dictionary<string, Dictionary<string, NuGetVersion>> GetFrameworkPackages(List<string> lockFilePaths)
        {
            try
            {
                var uniqueTargets = new Dictionary<NuGetFramework, (HashSet<string> References, LockFileTarget LockFileTarget)>();

                foreach (var lockFilePath in lockFilePaths)
                {
                    var lockFile = new LockFileFormat().Read(lockFilePath);
                    foreach (var target in lockFile.Targets)
                    {
                        var frameworkReferences = GetFrameworkReferences(lockFile, target);
                        if (!uniqueTargets.TryGetValue(target.TargetFramework, out var value))
                        {
                            value = (new HashSet<string>(), target);
                            uniqueTargets[target.TargetFramework] = value;
                        }
                        foreach (var reference in frameworkReferences)
                        {
                            value.References.Add(reference);
                        }
                    }
                }

                var frameworkPackagesType = LoadAssembly();

                if (frameworkPackagesType == null)
                {
                    Logger.Warn("Assembly type 'Microsoft.ComponentDetection.Detectors.NuGet.FrameworkPackages' could not be found.");
                }

                var getFrameworkPackagesMethod = GetFrameworkPackagesMethod(frameworkPackagesType);

                if (getFrameworkPackagesMethod == null)
                {
                    Logger.Logger.Log(null, Level.Notice, $"Method 'GetFrameworkPackages' not found.", null);
                    return _foundFrameworkPackages;
                }

                foreach (var target in uniqueTargets)
                {
                    InvokeGetFrameworkPackagesMethod(
                        getFrameworkPackagesMethod,
                        target.Key,
                        [.. target.Value.References],
                        target.Value.LockFileTarget);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Debug("GetFrameworkPackages: Argument error", ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug("GetFrameworkPackages : Invalid operation", ex);
            }

            return _foundFrameworkPackages;
        }

        /// <summary>
        /// Returns framework references declared in the lock file target and corresponding package spec.
        /// </summary>
        /// <param name="lockFile">Lock file containing PackageSpec and targets.</param>
        /// <param name="target">Target within the lock file to inspect.</param>
        /// <returns>An array of framework reference names, or empty array when none found.</returns>
        public string[] GetFrameworkReferences(LockFile lockFile, LockFileTarget target)
        {
            var frameworkInformation = lockFile.PackageSpec.TargetFrameworks.FirstOrDefault(x => x.FrameworkName.Equals(target.TargetFramework));

            if (frameworkInformation == null)
            {
                return Array.Empty<string>();
            }

            var results = frameworkInformation.FrameworkReferences.Select(x => x.Name)
                .Concat(target.Libraries.SelectMany(l => l.FrameworkReferences))
                .Distinct()
                .ToArray();

            return results;
        }
        #endregion

        #region private methods
        /// <summary>
        /// Loads the Microsoft.ComponentDetection.Detectors assembly and returns the FrameworkPackages type when available.
        /// </summary>
        /// <returns>Type of the FrameworkPackages detector or null when the assembly/type cannot be loaded.</returns>
        private static Type LoadAssembly()
        {
            try
            {
                Assembly componentDetectionAssembly = Assembly.Load("Microsoft.ComponentDetection.Detectors");
                return componentDetectionAssembly.GetType("Microsoft.ComponentDetection.Detectors.NuGet.FrameworkPackages");
            }
            catch (FileNotFoundException ex)
            {
                Logger.Debug("LoadAssembly: FileNotFoundException", ex);
            }
            catch (FileLoadException ex)
            {
                Logger.Debug("LoadAssembly: FileLoadException", ex);
            }
            return null;
        }

        /// <summary>
        /// Locates the static public GetFrameworkPackages method on the detector type via reflection.
        /// </summary>
        /// <param name="frameworkPackagesType">Type to inspect for the method.</param>
        /// <returns><see cref="MethodInfo"/> for GetFrameworkPackages or null when not found.</returns>
        private static MethodInfo GetFrameworkPackagesMethod(Type frameworkPackagesType)
        {
            var methods = frameworkPackagesType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            if (methods != null && methods.Any(m => m.Name == "GetFrameworkPackages"))
            {
                return frameworkPackagesType.GetMethod("GetFrameworkPackages", BindingFlags.Static | BindingFlags.Public);
            }
            return null;
        }

        /// <summary>
        /// Invokes the detected GetFrameworkPackages method and processes returned framework package objects.
        /// </summary>
        /// <param name="getFrameworkPackagesMethod">MethodInfo representing the method to invoke.</param>
        /// <param name="targetFramework">Target framework to pass as the first argument.</param>
        /// <param name="frameworkReferences">Framework references to pass as the second argument.</param>
        /// <param name="lockFileTarget">LockFileTarget instance to pass as the third argument (may be null).</param>
        private void InvokeGetFrameworkPackagesMethod(MethodInfo getFrameworkPackagesMethod, NuGetFramework targetFramework, string[] frameworkReferences, LockFileTarget lockFileTarget)
        {
            object[] parameters = { targetFramework, frameworkReferences, lockFileTarget };
            var result = getFrameworkPackagesMethod.Invoke(null, parameters);

            if (result is Array frameworkPackagesArray)
            {
                foreach (var frameworkPackage in frameworkPackagesArray)
                {
                    ProcessFrameworkPackage(targetFramework, frameworkPackage);
                }
            }
        }

        /// <summary>
        /// Extracts framework name and package dictionary from a frameworkPackage object returned by the detector and stores it.
        /// </summary>
        /// <param name="targetFramework">Target framework associated with the frameworkPackage.</param>
        /// <param name="frameworkPackage">Framework package object returned by the detector.</param>
        private void ProcessFrameworkPackage(NuGetFramework targetFramework, object frameworkPackage)
        {
            var frameworkNameProperty = frameworkPackage.GetType().GetProperty("FrameworkName");
            var packagesProperty = frameworkPackage.GetType().GetProperty("Packages");

            if (frameworkNameProperty != null && packagesProperty != null)
            {
                var frameworkName = frameworkNameProperty.GetValue(frameworkPackage)?.ToString() ?? string.Empty;
                _foundFrameworkPackages[targetFramework + "-" + frameworkName] = packagesProperty.GetValue(frameworkPackage) as Dictionary<string, NuGetVersion> ?? new Dictionary<string, NuGetVersion>();
            }
        }
        #endregion
    }
}