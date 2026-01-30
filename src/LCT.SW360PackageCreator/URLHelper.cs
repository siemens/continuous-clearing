// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// URL Helper
    /// </summary>
    public partial class UrlHelper : IUrlHelper, IDisposable
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly HttpClient httpClient = new HttpClient();
        public static string GithubUrl { get; set; } = string.Empty;
        public static UrlHelper Instance { get; } = new UrlHelper();
        public CommonAppSettings CommonAppSettings { get; } = new CommonAppSettings();

        private const string SrcUrlFailWarnFormat = "Identification of SRC url failed for {0}, Exclude if it is an internal component or manually update the SRC url";

        private bool _disposed;

        /// <summary>
        /// Gets the SourceUrl For Alpine Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <param name="bomRef"></param>
        /// <returns>Components</returns>
        public async Task<Components> GetSourceUrlForAlpinePackage(string componentName, string componenVersion, string bomRef)
        {
            Components componentsData = new Components();
            try
            {
                string localPathforSourceRepo = GetDownloadPathForAlpineRepo();
                string fullPath = Path.Combine(localPathforSourceRepo, "aports");
                var alpineDistro = GetAlpineDistro(bomRef);
                if (!Directory.Exists(fullPath))
                {
                    //Clone AlpineRepository
                    CloneSource(localPathforSourceRepo, alpineDistro, fullPath);
                }
                else
                {
                    //Checkout stable branch
                    CheckoutDistro(alpineDistro, fullPath);
                }

                AlpinePackage alpinePackSourceDetails = await GetAlpineSourceUrl(componentName, componenVersion, localPathforSourceRepo);
                componentsData.AlpineSourceData = alpinePackSourceDetails.SourceDataForAlpine;
                componentsData.Name = alpinePackSourceDetails.Name;
                componentsData.Version = AlpineComponentVersionRegex().Replace(alpinePackSourceDetails.Version, "");
                componentsData.ReleaseExternalId = GetReleaseExternalIdForAlpine(alpinePackSourceDetails.Name, alpinePackSourceDetails.Version);
                componentsData.ComponentExternalId = GetComponentExternalIdForAlpine(alpinePackSourceDetails.Name);
                componentsData.SourceUrl = string.IsNullOrEmpty(alpinePackSourceDetails.SourceUrl) ? Dataconstant.SourceUrlNotFound : alpinePackSourceDetails.SourceUrl;
                componentsData.DownloadUrl = componentsData.SourceUrl.Equals(Dataconstant.SourceUrlNotFound) ? Dataconstant.DownloadUrlNotFound : componentsData.SourceUrl;
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceUrlForAlpinePackage", $"MethodName:GetSourceUrlForAlpinePackage(), ComponentName: {componentName}, Version: {componenVersion}", ex, "An I/O error occurred while processing the Alpine package source URL.");
            }
            return componentsData;
        }

        /// <summary>
        /// Gets Alpine Distro
        /// </summary>
        /// <param name="bomRef"></param>
        /// <returns>distro</returns>
        public static string GetAlpineDistro(string bomRef)
        {

            string[] getDistro = bomRef.Split("distro");
            string[] getDestroVersion = getDistro[1].Split("-");
            var output = getDestroVersion[1][..^2];
            var distro = output + "-stable";
            return distro;
        }

        /// <summary>
        /// Gets Alpine Source Url
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="localPathforSourceRepo"></param>
        /// <returns>AlpinePackage</returns>
        private static Task<AlpinePackage> GetAlpineSourceUrl(string name, string version, string localPathforSourceRepo)
        {
            AlpinePackage sourceURLDetails = new AlpinePackage { Name = name, Version = version };
            try
            {
                var pkgFolderName = localPathforSourceRepo + Dataconstant.ForwardSlash + "aports" + Dataconstant.ForwardSlash + "main" + Dataconstant.ForwardSlash + name;
                if (Directory.Exists(pkgFolderName))
                {
                    var pkgFilePath = localPathforSourceRepo + Dataconstant.ForwardSlash + "aports" + Dataconstant.ForwardSlash + "main" + Dataconstant.ForwardSlash + name + Dataconstant.ForwardSlash + "APKBUILD";
                    if (File.Exists(pkgFilePath))
                    {
                        var sourceData = GetSourceFromAPKBUILD(localPathforSourceRepo, name);
                        sourceURLDetails.SourceDataForAlpine = sourceData;
                        var sourceUrl = GetSourceUrlForAlpine(pkgFilePath, sourceData);
                        if (sourceUrl.EndsWith(FileConstant.TargzFileExtension) || sourceUrl.EndsWith(FileConstant.XzFileExtension) || sourceUrl.EndsWith(FileConstant.TgzFileExtension) || sourceUrl.EndsWith(FileConstant.Bz2FileExtension))
                        {
                            sourceURLDetails.SourceUrl = sourceUrl;
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetAlpineSourceUrl", $"MethodName:GetAlpineSourceUrl(), PackageName: {name}, Version: {version}, LocalPath: {localPathforSourceRepo}", ex, "An I/O error occurred while trying to retrieve the Alpine source URL.");
                Logger.Error($"GetAlpineSourceUrl() ", ex);
            }
            return Task.FromResult(sourceURLDetails);
        }

        /// <summary>
        /// Gets Source From APK BUILD
        /// </summary>
        /// <param name="localPathforSourceRepo"></param>
        /// <param name="name"></param>
        /// <returns>source data</returns>
        public static string GetSourceFromAPKBUILD(string localPathforSourceRepo, string name)
        {
            string sourceData = string.Empty;
            string pkgFilePath = string.Empty;
            try
            {
                var pkgFolderName = localPathforSourceRepo + Dataconstant.ForwardSlash + "aports" + Dataconstant.ForwardSlash + "main" + Dataconstant.ForwardSlash + name;
                if (Directory.Exists(pkgFolderName))
                {
                    pkgFilePath = localPathforSourceRepo + Dataconstant.ForwardSlash + "aports" + Dataconstant.ForwardSlash + "main" + Dataconstant.ForwardSlash + name + Dataconstant.ForwardSlash + "APKBUILD";
                    if (File.Exists(pkgFilePath))
                    {
                        var apkBuildTxt = File.ReadAllText(pkgFilePath);
                        int sourceLength = apkBuildTxt.IndexOf("source=\"") + "source=\"".Length;
                        int txtLengthFrom = apkBuildTxt.Length - sourceLength;
                        string textFrom = apkBuildTxt.Substring(sourceLength, txtLengthFrom);
                        int textLengthTo = textFrom.IndexOf('\"') - 1 + 1;
                        sourceData = textFrom[..textLengthTo];
                    }
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceFromAPKBUILD", $"MethodName:GetSourceFromAPKBUILD(), PackageName: {name}, LocalPath: {localPathforSourceRepo}", ex, "An I/O error occurred while trying to read the APKBUILD file.");
                Logger.Error("GetSourceFromAPKBUILD() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceFromAPKBUILD", $"MethodName:GetSourceFromAPKBUILD(), PackageName: {name}, LocalPath: {localPathforSourceRepo}", ex, "Unauthorized access occurred while trying to read the APKBUILD file.");
                Logger.Error($"GetSourceFromAPKBUILD() ", ex);
            }

            return sourceData;
        }

        /// <summary>
        /// Gets Source Url For Alpine
        /// </summary>
        /// <param name="pkgFilePath"></param>
        /// <param name="sourceData"></param>
        /// <returns>source url</returns>
        public static string GetSourceUrlForAlpine(string pkgFilePath, string sourceData)
        {
            string sourceUrl = string.Empty;
            try
            {
                var pkgVersionLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("pkgver"));
                var pkgNameLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("pkgname"));
                var _commitLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("_commit"));
                var _tzcodeverLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("_tzcodever"));
                string pkgVersion = string.Empty;
                string pkgName = string.Empty;
                string _commitValue = string.Empty;
                string _tzcodever = string.Empty;

                if (pkgVersionLine != null)
                {
                    string[] pkgVersionValue = AlpinePackagelineRegex().Split(pkgVersionLine);
                    pkgVersion = pkgVersionValue[1];
                }
                if (pkgNameLine != null)
                {
                    string[] pkgNameData = AlpinePackagelineRegex().Split(pkgNameLine);
                    pkgName = pkgNameData[1];
                }
                if (_tzcodeverLine != null)
                {
                    string[] _tzcodeverValue = AlpinePackagelineRegex().Split(_tzcodeverLine);
                    _tzcodever = _tzcodeverValue[1];
                    if (_tzcodeverLine.Contains("pkgver"))
                    {
                        _tzcodever = pkgVersion;
                    }
                }
                if (_commitLine != null)
                {
                    string[] _commit = AlpinePackagelineRegex().Split(_commitLine);
                    _commitValue = _commit[1].Trim('"');
                }
                if (sourceData.Contains("https"))
                {
                    Match url = AlpineSourceDataRegex().Match(sourceData);
                    string finalUrl = url.ToString();
                    sourceUrl = finalUrl.Replace("$pkgver", pkgVersion).Replace("$pkgname", pkgName).Replace("$_commit", _commitValue).Replace("$_tzcodever", _tzcodever);
                    if (pkgVersion == null && _commitValue == null && _tzcodever == null)
                    {
                        sourceUrl = string.Empty;
                    }
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceUrlForAlpine", $"MethodName:GetSourceUrlForAlpine(), PackageFilePath: {pkgFilePath}", ex, "An I/O error occurred while trying to read the package file.");
                Logger.Error($"GetSourceUrlForAlpine() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceUrlForAlpine", $"MethodName:GetSourceUrlForAlpine(), PackageFilePath: {pkgFilePath}", ex, "Unauthorized access occurred while trying to read the package file.");
                Logger.Error($"GetSourceUrlForAlpine() ", ex);
            }

            return sourceUrl;
        }

        /// <summary>
        /// Gets Download Path For Alpine Repo
        /// </summary>
        /// <returns>local path for repo</returns>
        public static string GetDownloadPathForAlpineRepo()
        {
            string localPathforSourceRepo = string.Empty;
            try
            {                
                localPathforSourceRepo = Path.Combine(
    Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
    "ClearingTool",
    "DownloadedFiles") + Path.DirectorySeparatorChar;
                if (!Directory.Exists(localPathforSourceRepo))
                {
                    localPathforSourceRepo = Directory.CreateDirectory(localPathforSourceRepo).ToString();
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetDownloadPathForAlpineRepo", "MethodName:GetDownloadPathForAlpineRepo()", ex, "An I/O error occurred while trying to create or access the directory.");
                Logger.Error($"GetDownloadPathForAlpineRepo() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetDownloadPathForAlpineRepo", "MethodName:GetDownloadPathForAlpineRepo()", ex, "Unauthorized access occurred while trying to create or access the directory.");
                Logger.Error($"GetDownloadPathForAlpineRepo() ", ex);
            }

            return localPathforSourceRepo;
        }

        /// <summary>
        /// Get Release External Id For Alpine
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>external id</returns>
        public static string GetReleaseExternalIdForAlpine(string name, string version)
        {

            return $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        /// <summary>
        /// Generates the external identifier URL for an Alpine Linux package component using the specified package
        /// name.
        /// </summary>       
        /// <param name="name">The name of the Alpine Linux package for which to generate the external identifier. Cannot be null or empty.</param>
        /// <returns>A string containing the external identifier URL for the specified Alpine Linux package.</returns>
        private static string GetComponentExternalIdForAlpine(string name)
        {
            return $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{name}?arch=source";
        }

        /// <summary>
        /// Clones a source Git repository to a specified local directory and checks out the specified Alpine
        /// distribution if the target path exists.
        /// </summary>       
        /// <param name="localPathforSourceRepo">The local file system path where the source repository will be cloned. Must be a valid, existing directory.</param>
        /// <param name="alpineDistro">The name or identifier of the Alpine distribution branch or tag to check out after cloning.</param>
        /// <param name="fullPath">The full file system path to the directory where the repository should be checked out. If this directory
        /// exists, the specified Alpine distribution is checked out.</param>
        private static void CloneSource(string localPathforSourceRepo, string alpineDistro, string fullPath)
        {
            Logger.DebugFormat("CloneSource(): Start cloneing from git - LocalPath: {0}, AlpineDistro: {1}, FullPath: {2}", localPathforSourceRepo, alpineDistro, fullPath);
            List<string> gitCommands = GetGitCloneCommands();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (string command in gitCommands)
                {
                    Logger.DebugFormat("CloneSource(): Executing Git command: {0}", command);
                    Process p = new Process();
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.FileName = Path.Combine(@"git");
                    p.StartInfo.Arguments = command;
                    p.StartInfo.WorkingDirectory = localPathforSourceRepo;

                    p.Start();
                    p.WaitForExit();
                    Logger.DebugFormat("CloneSource(): Git command completed with ExitCode: {0}", p.ExitCode);

                }
            }
            else
            {
                Logger.DebugFormat("CloneSource(): Executing Git command: {0}", gitCommands[1]);
                Process p = new Process();
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.FileName = Path.Combine(@"git");
                p.StartInfo.Arguments = gitCommands[1];
                p.StartInfo.WorkingDirectory = localPathforSourceRepo;

                p.Start();
                p.WaitForExit();
                Logger.DebugFormat("CloneSource(): Git command completed with ExitCode: {0}", p.ExitCode);
            }
            if (Directory.Exists(fullPath))
            {
                Logger.DebugFormat("CloneSource(): Directory exists at {0}, proceeding to checkout distro.", fullPath);
                CheckoutDistro(alpineDistro, fullPath);
            }
            Logger.DebugFormat("CloneSource(): completed cloneing - LocalPath: {0}, AlpineDistro: {1}, FullPath: {2}", localPathforSourceRepo, alpineDistro, fullPath);
        }

        /// <summary>
        /// Check out Distro
        /// </summary>
        /// <param name="alpineDistro"></param>
        /// <param name="fullPath"></param>
        private static void CheckoutDistro(string alpineDistro, string fullPath)
        {
            Logger.DebugFormat("CheckoutDistro(): Start checkout github repo - AlpineDistro: {0}, FullPath: {1}", alpineDistro, fullPath);
            Process p = new Process();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = Path.Combine(@"git");
            p.StartInfo.Arguments = $"checkout" + " " + alpineDistro;
            p.StartInfo.WorkingDirectory = fullPath;

            p.Start();
            p.WaitForExit();
            Logger.DebugFormat("CheckoutDistro(): Git checkout completed with ExitCode: {0}", p.ExitCode);
            if (p.ExitCode != 0)
            {
                string errorOutput = p.StandardError.ReadToEnd();
                Logger.ErrorFormat("CheckoutDistro(): Git checkout failed for AlpineDistro: {0}, FullPath: {1}, Error: {2}", alpineDistro, fullPath, errorOutput);
            }
            Logger.DebugFormat("CheckoutDistro(): Completed checkout github repo - AlpineDistro: {0}, FullPath: {1}", alpineDistro, fullPath);
        }

        /// <summary>
        /// Get Git Clone Commands
        /// </summary>
        /// <returns></returns>
        private static List<string> GetGitCloneCommands()
        {
            return new List<string>()
           {
               $"config --global core.protectNTFS false",
               $"clone" +" "+ CommonAppSettings.AlpineAportsGitURL,
           };
        }

        /// <summary>
        /// Gets the SourceUrl For Debian Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <returns>Components</returns>
        public async Task<Components> GetSourceUrlForDebianPackage(string componentName, string componenVersion)
        {
            Components componentsData = new Components();
            DebianPackage debianPackSourceDetails = await GetSourceUrl(componentName, componenVersion);

            if (debianPackSourceDetails.IsRetryRequired)
            {
                Logger.DebugFormat("Retry for.. {0}-{1}", componentName, componenVersion);
                debianPackSourceDetails = await RetryToGetSourceURlDetailsAsync(componentName, componenVersion);
            }

            componentsData.Name = debianPackSourceDetails.Name;
            componentsData.Version = DebianComponentVersionRegex().Replace(debianPackSourceDetails.Version, "");
            componentsData.ReleaseExternalId = GetReleaseExternalId(debianPackSourceDetails.Name, debianPackSourceDetails.Version);
            componentsData.ComponentExternalId = GetComponentExternalId(debianPackSourceDetails.Name);
            componentsData.SourceUrl = string.IsNullOrEmpty(debianPackSourceDetails.SourceUrl) ? Dataconstant.SourceUrlNotFound : debianPackSourceDetails.SourceUrl;
            componentsData.PatchURLs = debianPackSourceDetails.PatchURLs;
            componentsData.DownloadUrl = componentsData.SourceUrl.Equals(Dataconstant.SourceUrlNotFound) ? Dataconstant.DownloadUrlNotFound : componentsData.SourceUrl;


            if (componentName != debianPackSourceDetails.Name || componenVersion != debianPackSourceDetails.Version)
            {
                Logger.DebugFormat("Source name found for binary package {0}-{1} -- Source name and version ==> {2}-{3}", componentName, componenVersion, debianPackSourceDetails.Name, debianPackSourceDetails.Version);
            }

            return componentsData;
        }

        /// <summary>
        /// Gets the Source Url For Nuget Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <param name="isDebugMode"></param>
        /// <returns>string</returns>
        public async Task<string> GetSourceUrlForNugetPackage(string componentName, string componenVersion)
        {
            Logger.DebugFormat("GetSourceUrlForNugetPackage(): Start identifying sourceUrl for Nuget Package - ComponentName: {0}, Version: {1}", componentName, componenVersion);
            string name = componentName.ToLowerInvariant();
            string version = componenVersion.ToLowerInvariant();
            string nuspecURL = $"{CommonAppSettings.SourceURLNugetApi}{name}/{version}/{name}.nuspec";
            Logger.DebugFormat("GetSourceUrlForNugetPackage(): Constructed NuSpec URL: {0}", nuspecURL);
            var sourceURL = await GetSourceURLFromNuspecFile(nuspecURL, componentName);
            Logger.DebugFormat("GetSourceUrlForNugetPackage(): Completed to identify sourceUrl for Nuget - ComponentName: {0}, Version: {1}, SourceURL: {2}", componentName, componenVersion, sourceURL);
            return sourceURL;
        }

        /// <summary>
        /// Gets the Source URL for NPM Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="version"></param>
        /// <returns>string</returns>
        public string GetSourceUrlForNpmPackage(string componentName, string version)
        {
            Logger.DebugFormat("GetSourceUrlForNpmPackage(): Start identifying sourceUrl for Npm Package - ComponentName: {0}, Version: {1}", componentName, version);

            string npmViewCommandToGetUrl = String.Empty;

            Process p = new Process();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                npmViewCommandToGetUrl = $"-c \" npm view {componentName}@{version} repository.url --registry https://registry.npmjs.org/ \"";
                p.StartInfo.FileName = FileConstant.DockerCMDTool;
                Logger.DebugFormat("GetSourceUrlForNpmPackage(): Linux OS detected. Command: {0}", npmViewCommandToGetUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                npmViewCommandToGetUrl = $"/c npm view {componentName}@{version} repository.url --registry https://registry.npmjs.org/";
                p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                Logger.DebugFormat("GetSourceUrlForNpmPackage(): Windows OS detected. Command: {0}", npmViewCommandToGetUrl);
            }
            else
            {
                Logger.Debug("GetSourceUrlForNpmPackage(): OS not recognized. Unable to determine the command to execute.");
            }


            p.StartInfo.Arguments = npmViewCommandToGetUrl;
            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo);
            Result result = processResult?.Result;
            string sourceUrl = result?.StdOut?.TrimEnd();
            Logger.DebugFormat("GetSourceUrlForNpmPackage(): NPM view command output - StdOut: {0}, StdErr: {1}", result?.StdOut, result?.StdErr);
            IRepository repo = new Repository();
            GithubUrl = repo.IdentifyRepoURLForGit(sourceUrl, componentName);

            Logger.DebugFormat("GetSourceUrlForNpmPackage(): Final GitHub URL for source code: {0}", GithubUrl);
            Logger.DebugFormat("GetSourceUrlForNpmPackage():Completed to identify sourceUrl - ComponentName: {0}, Version: {1}", componentName, version);

            return GithubUrl;
        }
        /// <summary>
        /// Gets the Source URL for CARGO Packages
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componentVersion"></param>
        /// <returns>string</returns>

        public async Task<string> GetSourceUrlForCargoPackage(string componentName, string componentVersion)
        {
            string downLoadUrl = $"{CommonAppSettings.SourceBaseUrlForCargo}{Dataconstant.ForwardSlash}{CommonAppSettings.SourceUrlForCargo}{componentName}{Dataconstant.ForwardSlash}{componentVersion}";
            string repositoryUrl = string.Empty;
            try
            {
                using var localHttpClient = new HttpClient();
                localHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ContinuousClearing");
                var response = await localHttpClient.GetAsync(downLoadUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jObj = JObject.Parse(json);
                    var versionToken = jObj["version"];
                    var dlPath = versionToken?["dl_path"];

                    if (dlPath != null && dlPath.Type != JTokenType.Null && !string.IsNullOrWhiteSpace(dlPath.ToString()))
                    {
                        repositoryUrl = dlPath.ToString();
                        repositoryUrl = $"{CommonAppSettings.SourceBaseUrlForCargo}{repositoryUrl}";
                    }
                    else
                    {
                        repositoryUrl = "";
                        Logger.WarnFormat(SrcUrlFailWarnFormat, componentName);
                    }
                }
                else
                {
                    Logger.WarnFormat(SrcUrlFailWarnFormat, componentName);
                    Logger.DebugFormat("GetSourceUrlForCargoPackage(): HTTP Status: {0} for URL: {1}", response.StatusCode, downLoadUrl);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug("GetSourceUrlForCargoPackage()", ex);
                Logger.WarnFormat(SrcUrlFailWarnFormat, componentName);
            }
            return repositoryUrl;
        }
        /// <summary>
        /// Gets the Source URL for CONAN Packages
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="version"></param>
        /// <returns>string</returns>
        public async Task<string> GetSourceUrlForConanPackage(string componentName, string componenVersion)
        {

            var downLoadUrl = $"{CommonAppSettings.SourceURLConan}" + componentName + "/all/conandata.yml";
            var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            string componentSrcURL = string.Empty;

            using (HttpClient _httpClient = new HttpClient())
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, downLoadUrl);
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var jsonObject = await response.Content.ReadAsStringAsync();
                    Sources packageSourcesInfo = deserializer.Deserialize<Sources>(jsonObject);
                    if (packageSourcesInfo.SourcesData.TryGetValue(componenVersion, out var release))
                    {
                        if (release.Url.GetType().Name.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                        {
                            componentSrcURL = release.Url.ToString();
                        }
                        else
                        {
                            List<object> urlList = (List<object>)release.Url;
                            componentSrcURL = urlList.FirstOrDefault()?.ToString() ?? "";
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Logger.WarnFormat(SrcUrlFailWarnFormat, componentName);
                    LogHandlingHelper.ExceptionErrorHandling("GetSourceUrlForConanPackage", $"MethodName:GetSourceUrlForConanPackage(), ComponentName: {componentName}, Version: {componenVersion}, URL: {downLoadUrl}", ex, "An HTTP request error occurred while trying to fetch the Conan package source URL.");
                }
                catch (YamlException ex)
                {
                    Logger.WarnFormat(SrcUrlFailWarnFormat, componentName);
                    LogHandlingHelper.ExceptionErrorHandling("GetSourceUrlForConanPackage", $"MethodName:GetSourceUrlForConanPackage(), ComponentName: {componentName}, Version: {componenVersion}, URL: {downLoadUrl}", ex, "A YAML parsing error occurred while trying to deserialize the Conan package data.");
                }
                catch (ArgumentNullException ex)
                {
                    LogHandlingHelper.ExceptionErrorHandling("GetSourceUrlForConanPackage", $"MethodName:GetSourceUrlForConanPackage(), ComponentName: {componentName}, Version: {componenVersion}, URL: {downLoadUrl}", ex, "A null argument was encountered while processing the Conan package source URL.");
                }
            }
            return componentSrcURL;
        }

        /// <summary>
        /// Gets Source URL From Nuspec File
        /// </summary>
        /// <param name="nuspecURL"></param>
        /// <param name="componentName"></param>
        /// <returns>task that represents asynchronous operation</returns>
        private async Task<string> GetSourceURLFromNuspecFile(string nuspecURL, string componentName)
        {
            string response;
            string url = string.Empty;
            GithubUrl = string.Empty;
            try
            {
                response = await httpClient.GetStringAsync(nuspecURL);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response);

                XmlNodeList nodeList = xmlDoc.GetElementsByTagName("repository");
                foreach (XmlNode node in nodeList)
                {
                    url = node.Attributes["url"]?.Value;
                }
                IRepository repo = new Repository();
                GithubUrl = repo.IdentifyRepoURLForGit(url, componentName);
                if (GithubUrl == string.Empty)
                {
                    GithubUrl = SearchForProjectURLTagInNuspecFile(xmlDoc);
                    return GithubUrl;
                }
            }
            catch (AggregateException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromNuspecFile", $"MethodName:GetSourceURLFromNuspecFile(), ComponentName: {componentName}, NuspecURL: {nuspecURL}", ex, "Multiple errors occurred while processing the Nuspec file. Please investigate the inner exceptions for more details.");
            }
            catch (HttpRequestException ex)
            {
                Logger.WarnFormat(SrcUrlFailWarnFormat, componentName);
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromNuspecFile", $"MethodName:GetSourceURLFromNuspecFile(), ComponentName: {componentName}, NuspecURL: {nuspecURL}", ex, "An HTTP request error occurred while trying to fetch the Nuspec file.");
            }
            return GithubUrl;
        }

        /// <summary>
        /// Search For Project URL Tag In Nuspec File
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns>url</returns>
        private static string SearchForProjectURLTagInNuspecFile(XmlDocument xmlDoc)
        {
            string url = string.Empty;

            XmlNodeList projectUrl = xmlDoc.GetElementsByTagName("projectUrl");

            foreach (XmlNode node in projectUrl)
            {
                url = node.InnerText;
            }
            if (url.StartsWith("https://github.com"))
            {
                return url;
            }
            XmlNodeList projectSourceUrl = xmlDoc.GetElementsByTagName("projectSourceUrl");
            if (projectSourceUrl.Count != 0)
            {
                foreach (XmlNode node in projectSourceUrl)
                {
                    url = node.InnerText;

                }
                return url;
            }

            return url;
        }

        /// <summary>
        /// Gets Release External Id
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>external id</returns>
        public static string GetReleaseExternalId(string name, string version)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{Dataconstant.PurlCheck()["DEBIAN"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        /// <summary>
        /// Gets Component External Id
        /// </summary>
        /// <param name="name"></param>
        /// <returns>external id</returns>
        private static string GetComponentExternalId(string name)
        {
            return $"{Dataconstant.PurlCheck()["DEBIAN"]}{Dataconstant.ForwardSlash}{name}?arch=source";
        }

        /// <summary>
        /// Gets Source Url
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>task</returns>
        private async Task<DebianPackage> GetSourceUrl(string name, string version)
        {
            DebianPackage sourceURLDetails = new DebianPackage { Name = name, Version = version };
            string packageType = "source";
            sourceURLDetails = await GetArchiveResponse(sourceURLDetails, packageType);

            if (string.IsNullOrEmpty(sourceURLDetails.JsonText))
            {
                packageType = "binary";
                sourceURLDetails = await GetArchiveResponse(sourceURLDetails, packageType);
            }

            if (!string.IsNullOrEmpty(sourceURLDetails.JsonText))
            {
                sourceURLDetails = await GetSourceDetialsFromType(packageType, sourceURLDetails);
            }

            return sourceURLDetails;
        }

        /// <summary>
        /// Gets Archive Response
        /// </summary>
        /// <param name="packageDetails"></param>
        /// <param name="packageType"></param>
        /// <returns>package</returns>
        private async Task<DebianPackage> GetArchiveResponse(DebianPackage packageDetails, string packageType)
        {
            string URL;
            try
            {
                // clearing previous responses for current component
                packageDetails.JsonText = string.Empty;

                if (packageType == "source")
                {
                    URL = $"{CommonAppSettings.SnapshotBaseURL}package/{packageDetails.Name}" +
                        $"{Dataconstant.ForwardSlash}{packageDetails.Version}{Dataconstant.SourceURLSuffix}";
                }
                else
                {
                    URL = $"{CommonAppSettings.SnapshotBaseURL}binary/{packageDetails.Name}{Dataconstant.ForwardSlash}";
                }
                Logger.DebugFormat("GetArchiveResponse(): Constructed URL for {0} package type: {1}", packageType, URL);
                var result = await httpClient.GetStringAsync(URL);
                packageDetails.JsonText = result.ToString();
                Logger.DebugFormat("GetArchiveResponse(): Successfully fetched response for Package Name: {0}, Package Type: {1}", packageDetails.Name, packageType);
                return packageDetails;
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetArchiveResponse", $"MethodName:GetArchiveResponse(), PackageName: {packageDetails.Name}, PackageType: {packageType}", ex, "An HTTP request error occurred while trying to fetch the archive response.");
                if (!ex.Message.Contains("404") && packageType == "source")
                {
                    packageDetails.IsRetryRequired = true;
                    Logger.DebugFormat("GetArchiveResponse:File Name : {0}, Added for Retry.", packageDetails.Name);
                }
            }
            return packageDetails;
        }

        /// <summary>
        /// Gets Source Detials From Type
        /// </summary>
        /// <param name="packageType"></param>
        /// <param name="sourceURLDetails"></param>
        /// <returns>debian package</returns>
        private async Task<DebianPackage> GetSourceDetialsFromType(string packageType, DebianPackage sourceURLDetails)
        {
            if (packageType == "source")
            {
                sourceURLDetails = GetSourceURLFromJsonTextForSourceType(sourceURLDetails);
            }
            else
            {
                sourceURLDetails = await GetSourceURLFromJsonTextForBinaryType(sourceURLDetails);
            }
            return sourceURLDetails;
        }

        /// <summary>
        /// Gets Source URL From Json Text For SourceType
        /// </summary>
        /// <param name="sourceURLDetails"></param>
        /// <returns>devbian package</returns>
        private static DebianPackage GetSourceURLFromJsonTextForSourceType(DebianPackage sourceURLDetails)
        {
            List<string> SourceURL = new List<string>();

            try
            {
                JObject data = JObject.Parse(sourceURLDetails.JsonText);
                JToken fileinformations = data["fileinfo"];

                foreach (JToken fileinfo in fileinformations.Children())
                {
                    List<string> uniqueFiles = new List<string>();

                    foreach (JToken dependencyToken in fileinfo.Children().Children())
                    {
                        DebianFileInfo debianFileInfo = dependencyToken.ToObject<DebianFileInfo>();

                        if (!uniqueFiles.Contains(debianFileInfo.name))
                        {
                            SourceURL.Add($"{CommonAppSettings.SnapshotDownloadURL}{debianFileInfo.archive_name}/{debianFileInfo.first_seen}" +
                                $"{debianFileInfo.path}/{debianFileInfo.name}");
                            uniqueFiles.Add(debianFileInfo.name);
                        }
                    }
                }

                if (SourceURL.Count > 0)
                {
                    sourceURLDetails = GetProperSourceURL(sourceURLDetails, SourceURL);
                }
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromJsonTextForSourceType", $"MethodName:GetSourceURLFromJsonTextForSourceType(), JsonText: {sourceURLDetails.JsonText}", ex, "An error occurred while parsing the JSON text.");
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromJsonTextForSourceType", $"MethodName:GetSourceURLFromJsonTextForSourceType(), JsonText: {sourceURLDetails.JsonText}", ex, "An I/O error occurred while processing the source URL.");
            }
            return sourceURLDetails;
        }

        /// <summary>
        /// Gets Source URL From Json Text For Binary Type
        /// </summary>
        /// <param name="sourceURLDetails"></param>
        /// <returns>debian package</returns>
        private async Task<DebianPackage> GetSourceURLFromJsonTextForBinaryType(DebianPackage sourceURLDetails)
        {
            try
            {
                JObject data = JObject.Parse(sourceURLDetails.JsonText);
                JToken fileinformations = data["result"];

                foreach (JToken dependencyToken in fileinformations.Children())
                {
                    string binary_version = dependencyToken.Value<string>("binary_version");
                    if (binary_version == sourceURLDetails.Version)
                    {
                        string source = dependencyToken.Value<string>("source");
                        string sourceVersion = dependencyToken.Value<string>("version");
                        sourceURLDetails = await GetArchiveResponse(new DebianPackage() { Name = source, Version = sourceVersion }, "source");
                        if (!string.IsNullOrEmpty(sourceURLDetails.JsonText))
                        {
                            sourceURLDetails.Name = source;
                            sourceURLDetails.Version = sourceVersion;
                            sourceURLDetails = GetSourceURLFromJsonTextForSourceType(sourceURLDetails);
                            break;
                        }
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromJsonTextForBinaryType", $"MethodName:GetSourceURLFromJsonTextForBinaryType(), JsonText: {sourceURLDetails.JsonText}", ex, "An error occurred while parsing the JSON text.");
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromJsonTextForBinaryType", $"MethodName:GetSourceURLFromJsonTextForBinaryType(), JsonText: {sourceURLDetails.JsonText}", ex, "An I/O error occurred while processing the binary type source URL.");
            }

            return sourceURLDetails;
        }

        /// <summary>
        /// Gets Proper Source URL
        /// </summary>
        /// <param name="sourceURLDetails"></param>
        /// <param name="URLs"></param>
        /// <returns>debian package</returns>
        private static DebianPackage GetProperSourceURL(DebianPackage sourceURLDetails, List<string> URLs)
        {
            if (URLs.Count > 2)
            {
                // 3 files required for patching i.e DSC,Orig & Debian/Diff files
                sourceURLDetails.PatchURLs = URLs.ToArray();
            }

            // Finding upstream source file(.orig.tar.*)
            string OrigFile = URLs.Find(item => item.Contains(FileConstant.OrigTarFileExtension) &&
                                                 (item.EndsWith(FileConstant.TargzFileExtension) ||
                                                 item.EndsWith(FileConstant.XzFileExtension) ||
                                                 item.EndsWith(FileConstant.TgzFileExtension) ||
                                                 item.EndsWith(FileConstant.Bz2FileExtension)));

            if (!string.IsNullOrEmpty(OrigFile))
            {
                sourceURLDetails.SourceUrl = OrigFile.ToString();
                sourceURLDetails.DownloadUrl = OrigFile.ToString();
            }
            else
            {
                // Finding actual source file ()
                var sourceURL = URLs.Find(item => !item.Contains(FileConstant.OrigTarFileExtension) && !item.Contains(FileConstant.DSCFileExtension)
                                    && !item.Contains(FileConstant.DebianTarFileExtension) && !item.Contains(FileConstant.DebianFileExtension)
                                    && (item.EndsWith(FileConstant.TargzFileExtension)
                                    || item.EndsWith(FileConstant.XzFileExtension)
                                    || item.EndsWith(FileConstant.TgzFileExtension)));

                if (!string.IsNullOrEmpty(sourceURL))
                {
                    sourceURLDetails.SourceUrl = sourceURL.ToString();
                    sourceURLDetails.DownloadUrl = sourceURL.ToString();
                }
                else
                {
                    // Not able to distinguish source file, so taking DSC file as source file
                    var dscFile = URLs.Find(item => item.Contains(FileConstant.DSCFileExtension));

                    if (!string.IsNullOrEmpty(dscFile))
                    {
                        sourceURLDetails.SourceUrl = dscFile.ToString();
                        sourceURLDetails.DownloadUrl = dscFile.ToString();
                    }
                    else
                    {
                        // If DSC file not found ,taking anyfile as source file for time being(i.e till applying patch)
                        sourceURLDetails.SourceUrl = URLs[0];
                        sourceURLDetails.DownloadUrl = URLs[0];
                    }
                }
            }
            return sourceURLDetails;
        }

        /// <summary>
        /// Retry To Get Source  URl Details Async
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>debian package</returns>
        private async Task<DebianPackage> RetryToGetSourceURlDetailsAsync(string name, string version)
        {
            Logger.DebugFormat("RetryToGetSourceURlDetailsAsync(): Start - ComponentName: {0}, Version: {1}", name, version);
            await Task.Delay(2000);
            DebianPackage sourceDetails = await GetSourceUrl(name, version);

            if (!string.IsNullOrEmpty(sourceDetails.SourceUrl))
            {
                Logger.DebugFormat("RetryToGetSourceURlDetailsAsync(): Retry successful for ComponentName: {0}, Version: {1}, SourceUrl: {2}", name, version, sourceDetails.SourceUrl);
            }
            else
            {
                Logger.DebugFormat("RetryToGetSourceURlDetailsAsync(): Source package not found for ComponentName: {0}, Version: {1}", name, version);
            }
            Logger.DebugFormat("RetryToGetSourceURlDetailsAsync(): End - ComponentName: {0}, Version: {1}", name, version);
            return sourceDetails;
        }

        /// <summary>
        /// Gets the Source Url For Python Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <param name="isDebugMode"></param>
        /// <returns>string</returns>
        public async Task<string> GetSourceUrlForPythonPackage(string componentName, string componentVersion)
        {
            Logger.DebugFormat("URLHelper.GetSourceUrlForPythonPackage():Started to identify source url for poetry package of this component:Name:{0},Version:{1}", componentName, componentVersion);
            string name = componentName.ToLowerInvariant();
            string version = componentVersion.ToLowerInvariant();
            var response = await GetResponseFromPyPiOrg(name, version);
            string sourceURL = GetSourceURLFromPyPiResponse(response);
            Logger.DebugFormat("URLHelper.GetSourceUrlForPythonPackage():Completed the source url for poetry package source url:{0}", sourceURL);
            return sourceURL;
        }

        /// <summary>
        /// Gets Response From PyPiOrg
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <returns>result</returns>
        private async Task<string> GetResponseFromPyPiOrg(string componentName, string componenVersion)
        {
            string URL;
            const string result = "";
            try
            {
                URL = $"{CommonAppSettings.PyPiURL}{componentName}" +
                    $"{Dataconstant.ForwardSlash}{componenVersion}{Dataconstant.ForwardSlash}json";
                await LogHandlingHelper.HttpRequestHandling("Request For source url", $"MethodName:GetResponseFromPyPiOrg(), ComponentName: {componentName}, Version: {componenVersion}", httpClient, URL);
                var response = await httpClient.GetStringAsync(URL);
                LogHandlingHelper.HttpResponseOfStringContent("Response from source url", $"MethodName:GetResponseFromPyPiOrg(), ComponentName: {componentName}, Version: {componenVersion}", response);
                return response.ToString();
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("HttpRequestException for while getting source url", $"MethodName:GetResponseFromPyPiOrg(), ComponentName: {componentName}, Version: {componenVersion}", ex, "An HTTP request error occurred while trying to fetch the response from PyPi.");
            }
            catch (TaskCanceledException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("TaskCanceledException for while getting source url", $"MethodName:GetResponseFromPyPiOrg(), ComponentName: {componentName}, Version: {componenVersion}", ex, "The request to PyPi was canceled, possibly due to a timeout.");
            }
            return result;
        }

        /// <summary>
        /// Gets Source URL From PyPi Response
        /// </summary>
        /// <param name="response"></param>
        /// <returns>source url</returns>
        private static string GetSourceURLFromPyPiResponse(string response)
        {
            string SourceURL = "";

            try
            {
                JObject data = JObject.Parse(response);
                JToken fileinformations = data["urls"];

                foreach (JToken fileinfo in fileinformations.Children())
                {
                    var url = fileinfo["url"];

                    if (!string.IsNullOrEmpty(url.ToString()) && (url.ToString().EndsWith(FileConstant.TargzFileExtension) || url.ToString().EndsWith(FileConstant.ZipFileExtension)))
                    {
                        SourceURL = url.ToString();
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromPyPiResponse", "MethodName:GetSourceURLFromPyPiResponse()", ex, "An error occurred while parsing the JSON response from PyPi.");
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetSourceURLFromPyPiResponse", "MethodName:GetSourceURLFromPyPiResponse()", ex, "An I/O error occurred while processing the PyPi response.");
            }
            return SourceURL;
        }

        /// <summary>
        /// Download File Async
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="downloadFilePath"></param>
        /// <returns>download path</returns>
        public static async Task<string> DownloadFileAsync(Uri uri, string downloadFilePath)
        {
            string downloadedPath = string.Empty;
            try
            {
                await RetryHttpClientHandler.ExecuteWithRetryAsync(async () =>
                {
                    using WebClient webClient = new();
                    await webClient.DownloadFileTaskAsync(uri, downloadFilePath);
                });
                downloadedPath = downloadFilePath;
                Logger.DebugFormat("DownloadFileFromSnapshotorgAsync:File Name : {0} ,Downloaded Successfully!!", Path.GetFileName(downloadFilePath));
            }
            catch (WebException webex)
            {
                LogHandlingHelper.ExceptionErrorHandling("DownloadFileAsync", $"MethodName:DownloadFileAsync(), FilePath: {downloadFilePath}, URI: {uri}", webex, "A network error occurred while trying to download the file.");
            }
            return downloadedPath;
        }

        /// <summary>
        /// Gets Correct File Extension
        /// </summary>
        /// <param name="sourceURL"></param>
        /// <returns></returns>
        public static string GetCorrectFileExtension(string sourceURL)
        {
            int idx = sourceURL.LastIndexOf(Dataconstant.ForwardSlash);
            string fullname = string.Empty;

            if (idx != -1)
            {
                fullname = sourceURL.Substring(idx + 1);
            }

            return fullname;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (httpClient != null)
            {
                httpClient.Dispose();
            }

            _disposed = true;
        }
        [GeneratedRegex(@"^[0-9]+:")]
        private static partial Regex DebianComponentVersionRegex();
        [GeneratedRegex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=$]*)?")]
        private static partial Regex AlpineSourceDataRegex();
        [GeneratedRegex(@"^[0-9]+:")]
        private static partial Regex AlpineComponentVersionRegex();
        [GeneratedRegex(@"\=")]
        private static partial Regex AlpinePackagelineRegex();
    }
}
