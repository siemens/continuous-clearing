// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

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
        readonly Dictionary<string, Dictionary<string, NuGetVersion>> _foundFrameworkPackages = [];
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region public methods
        public Dictionary<string, Dictionary<string, NuGetVersion>> GetFrameworkPackages(List<string> lockFilePaths)
        {
            try
            {
                var uniqueTargets = new Dictionary<NuGetFramework, HashSet<string>>();

                foreach (var lockFilePath in lockFilePaths)
                {
                    var lockFile = new LockFileFormat().Read(lockFilePath);
                    foreach (var target in lockFile.Targets)
                    {
                        var frameworkReferences = GetFrameworkReferences(lockFile, target);
                        if (!uniqueTargets.TryGetValue(target.TargetFramework, out HashSet<string> value))
                        {
                            value = new HashSet<string>();
                            uniqueTargets[target.TargetFramework] = value;
                        }
                        foreach (var reference in frameworkReferences)
                        {
                            value.Add(reference);
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
                    InvokeGetFrameworkPackagesMethod(getFrameworkPackagesMethod, target.Key, target.Value.ToArray(), null);
                }
            }
            catch (FileNotFoundException ex)
            {
                Logger.Debug($"GetFrameworkPackages: FileNotFoundException  {ex.Message}");
            }
            catch (FileLoadException ex)
            {
                Logger.Debug($"GetFrameworkPackages: FileLoadException {ex.Message}");
            }
            catch (TypeLoadException ex)
            {
                Logger.Debug($"GetFrameworkPackages : Not able to load Microsoft.ComponentDetection.Detectors.NuGet.FrameworkPackages assembly.: {ex.Message}");
            }
            catch (ArgumentNullException ex)
            {
                Logger.Debug($"GetFrameworkPackages : Argument null: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                Logger.Debug($"GetFrameworkPackages: Argument error: {ex.Message}");
            }
            catch (NullReferenceException ex)
            {
                Logger.Debug($"GetFrameworkPackages : Null reference: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"GetFrameworkPackages : Invalid operation: {ex.Message}");
            }

            return _foundFrameworkPackages;
        }

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
        private static Type LoadAssembly()
        {
            Assembly componentDetectionAssembly = Assembly.Load("Microsoft.ComponentDetection.Detectors");
            return componentDetectionAssembly.GetType("Microsoft.ComponentDetection.Detectors.NuGet.FrameworkPackages");
        }

        private static MethodInfo GetFrameworkPackagesMethod(Type frameworkPackagesType)
        {
            var methods = frameworkPackagesType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (methods != null && methods.Any(m => m.Name == "GetFrameworkPackages"))
            {
                return frameworkPackagesType.GetMethod("GetFrameworkPackages", BindingFlags.Static | BindingFlags.Public);
            }
            return null;
        }

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