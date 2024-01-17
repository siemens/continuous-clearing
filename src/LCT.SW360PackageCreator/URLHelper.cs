// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using JetBrains.Annotations;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using Microsoft.PowerShell.Commands;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Management.Automation.Runspaces;
using NuGet.ProjectModel;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// URL Helper
    /// </summary>
    public class UrlHelper : IUrlHelper, IDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly HttpClient httpClient = new HttpClient();
        public static string GithubUrl { get; set; } = string.Empty;
        public static UrlHelper Instance { get; } = new UrlHelper();
        public CommonAppSettings CommonAppSettings { get; } = new CommonAppSettings();

        private bool _disposed;

        /// <summary>
        /// Gets the SourceUrl For Alpine Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <returns>Components</returns>
        public async Task<Components> GetSourceUrlForAlpinePackage(string componentName, string componenVersion)
        {
            Components componentsData = new Components();
            try
            {
                string localPathforSourceRepo = GetDownloadPathForAlpineRepo();
                string fullPath = Path.Combine(localPathforSourceRepo, "aports");
                if (!Directory.Exists(fullPath))
                {
                    //Clone AlpineRepository
                    CloneSource(localPathforSourceRepo);
                }

                AlpinePackage alpinePackSourceDetails = await GetAlpineSourceUrl(componentName, componenVersion, localPathforSourceRepo);
                componentsData.Name = alpinePackSourceDetails.Name;
                componentsData.Version = Regex.Replace(alpinePackSourceDetails.Version, @"^[0-9]+:", "");
                componentsData.ReleaseExternalId = GetReleaseExternalIdForAlpine(alpinePackSourceDetails.Name, alpinePackSourceDetails.Version);
                componentsData.ComponentExternalId = GetComponentExternalIdForAlpine(alpinePackSourceDetails.Name);
                componentsData.SourceUrl = string.IsNullOrEmpty(alpinePackSourceDetails.SourceUrl) ? Dataconstant.SourceUrlNotFound : alpinePackSourceDetails.SourceUrl;
                componentsData.DownloadUrl = componentsData.SourceUrl.Equals(Dataconstant.SourceUrlNotFound) ? Dataconstant.DownloadUrlNotFound : componentsData.SourceUrl;
            }
            catch (IOException ex)
            {
                Logger.Error($"GetAlpineSourceUrl() ", ex);
            }
            return componentsData;
        }

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
                Logger.Error($"GetAlpineSourceUrl() ", ex);
            }
            return Task.FromResult(sourceURLDetails);
        }
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
                        int textLengthTo = textFrom.IndexOf("\"") - "\"".Length + 1;
                        sourceData = textFrom.Substring(0, textLengthTo);
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"GetSourceFromAPKBUILD() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"GetSourceFromAPKBUILD() ", ex);
            }

            return sourceData;
        }
        public static string GetSourceUrlForAlpine(string pkgFilePath, string sourceData)
        {
            string sourceUrl = string.Empty;
            try
            {
                var pkgVersionLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("pkgver"));
                var _commitLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("_commit"));
                var _tzcodeverLine = File.ReadLines(pkgFilePath).FirstOrDefault(x => x.StartsWith("_tzcodever"));
                string pkgVersion = string.Empty;
                string _commitValue = string.Empty;
                string _tzcodever = string.Empty;

                if (pkgVersionLine != null)
                {
                    string[] pkgVersionValue = Regex.Split(pkgVersionLine, @"\=");
                    pkgVersion = pkgVersionValue[1];
                }
                if (_tzcodeverLine != null)
                {
                    string[] _tzcodeverValue = Regex.Split(_tzcodeverLine, @"\=");
                    _tzcodever = _tzcodeverValue[1];
                }
                if (_commitLine != null)
                {
                    string[] _commit = Regex.Split(_commitLine, @"\=");
                    _commitValue = _commit[1].Trim('"');
                }
                if (sourceData.Contains("https"))
                {
                    Match url = Regex.Match(sourceData, @"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=$]*)?");
                    string finalUrl = url.ToString();
                    sourceUrl = finalUrl.Replace("$pkgver", pkgVersion).Replace("$_commit", _commitValue).Replace("$_tzcodever", _tzcodever);
                    if (pkgVersion == null && _commitValue == null && _tzcodever == null)
                    {
                        sourceUrl = string.Empty;
                    }
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"GetDownloadPathForAlpineRepo() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"GetDownloadPathForAlpineRepo() ", ex);
            }

            return sourceUrl;
        }

        public static string GetDownloadPathForAlpineRepo()
        {
            string localPathforSourceRepo = string.Empty;
            try
            {
                localPathforSourceRepo = $"{Directory.GetParent(Directory.GetCurrentDirectory())}/ClearingTool/DownloadedFiles/";
            }
            catch (IOException ex)
            {
                Logger.Error($"GetDownloadPathForAlpineRepo() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"GetDownloadPathForAlpineRepo() ", ex);
            }

            return localPathforSourceRepo;
        }
        public static string GetReleaseExternalIdForAlpine(string name, string version)
        {

            return $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        private static string GetComponentExternalIdForAlpine(string name)
        {
            return $"{Dataconstant.PurlCheck()["ALPINE"]}{Dataconstant.ForwardSlash}{name}?arch=source";
        }

        private static void CloneSource(string localPathforSourceRepo)
        {
            List<string> gitCommands = GetGitCloneCommands();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (string command in gitCommands)
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            CreateNoWindow = true,
                            FileName = "git",
                            Arguments = command,
                            WorkingDirectory = localPathforSourceRepo,
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }
            else
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        CreateNoWindow = true,
                        FileName = "git",
                        Arguments = gitCommands[1],
                        WorkingDirectory = localPathforSourceRepo,
                    }
                };
                process.Start();
                process.WaitForExit();
            }

        }

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
                Logger.Debug($"Retry for.. {componentName}-{componenVersion}");
                debianPackSourceDetails = await RetryToGetSourceURlDetailsAsync(componentName, componenVersion);
            }

            componentsData.Name = debianPackSourceDetails.Name;
            componentsData.Version = Regex.Replace(debianPackSourceDetails.Version, @"^[0-9]+:", "");
            componentsData.ReleaseExternalId = GetReleaseExternalId(debianPackSourceDetails.Name, debianPackSourceDetails.Version);
            componentsData.ComponentExternalId = GetComponentExternalId(debianPackSourceDetails.Name);
            componentsData.SourceUrl = string.IsNullOrEmpty(debianPackSourceDetails.SourceUrl) ? Dataconstant.SourceUrlNotFound : debianPackSourceDetails.SourceUrl;
            componentsData.PatchURLs = debianPackSourceDetails.PatchURLs;
            componentsData.DownloadUrl = componentsData.SourceUrl.Equals(Dataconstant.SourceUrlNotFound) ? Dataconstant.DownloadUrlNotFound : componentsData.SourceUrl;


            if (componentName != debianPackSourceDetails.Name || componenVersion != debianPackSourceDetails.Version)
            {
                Logger.Debug($"Source name found for binary package {componentName}-{componenVersion} " +
                    $"-- Source name and version ==> {debianPackSourceDetails.Name}-{debianPackSourceDetails.Version}");
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
            Logger.Debug($"URLHelper.GetSourceUrlForNugetPackage():Start");
            string name = componentName.ToLowerInvariant();
            string version = componenVersion.ToLowerInvariant();
            string nuspecURL = $"{CommonAppSettings.SourceURLNugetApi}{name}/{version}/{name}.nuspec";
            var sourceURL = await GetSourceURLFromNuspecFile(nuspecURL, componentName);
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
            Logger.Debug($"GetSourceUrl():Start");

            string npmViewCommandToGetUrl = String.Empty;
            Logger.Debug($"GetSourceUrl():{npmViewCommandToGetUrl}");

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
                Logger.Debug($"GetSourceUrlForNpmPackage():Linux OS Found!!");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                npmViewCommandToGetUrl = $"/c npm view {componentName}@{version} repository.url --registry https://registry.npmjs.org/";
                p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                Logger.Debug($"GetSourceUrlForNpmPackage():Windows OS Found!!");
            }
            else
            {
                Logger.Debug($"GetSourceUrlForNpmPackage():OS Details not Found!!");
            }


            p.StartInfo.Arguments = npmViewCommandToGetUrl;
            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo);
            Result result = processResult?.Result;
            string sourceUrl = result?.StdOut?.TrimEnd();
            IRepository repo = new Repository();
            GithubUrl = repo.IdentifyRepoURLForGit(sourceUrl, componentName);

            Logger.Debug($"GetSourceUrl():Release Name : {componentName}@{version}, NPM view Output:{result?.StdOut},  Error  : {result?.StdErr}");
            Logger.Debug($"GetSourceUrl():End");

            return GithubUrl;
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
            Sources packageSourcesInfo=new Sources();
            using (HttpClient _httpClient=new HttpClient())
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, downLoadUrl);
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var jsonObject = await response.Content.ReadAsStringAsync();
                    packageSourcesInfo = deserializer.Deserialize<Sources>(jsonObject);
                    if (packageSourcesInfo.SourcesData.TryGetValue(componenVersion, out var release))
                    {
                        if (release.Url.GetType().Name.ToLowerInvariant() == "string")
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
                    Logger.Warn($"Identification of SRC url failed for {componentName}, " +
                                    $"Exclude if it is an internal component or manually update the SRC url");
                    Logger.Debug($"GetSourceUrlForConanPackage()", ex);
                }
                catch(YamlException ex)
                {
                    Logger.Warn($"Identification of SRC url failed for {componentName}, " +
                                    $"Exclude if it is an internal component or manually update the SRC url");
                    Logger.Debug($"GetSourceUrlForConanPackage()", ex);
                }
                catch (ArgumentNullException ex)
                {
                    Logger.Debug($"GetSourceUrlForConanPackage()", ex);
                }
            }
            return componentSrcURL;
        }

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
                Logger.Debug($"GetSourceURLFromNuspecFile():", ex);
            }
            catch (HttpRequestException ex)
            {
                Logger.Warn($"Identification of SRC url failed for {componentName}, " +
            $"Exclude if it is an internal component or manually update the SRC url");
                Logger.Debug($"GetSourceURLFromNuspecFile()", ex);
            }
            return GithubUrl;
        }

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

        public static string GetReleaseExternalId(string name, string version)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{Dataconstant.PurlCheck()["DEBIAN"]}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        private static string GetComponentExternalId(string name)
        {
            return $"{Dataconstant.PurlCheck()["DEBIAN"]}{Dataconstant.ForwardSlash}{name}?arch=source";
        }

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

                var result = await httpClient.GetStringAsync(URL);
                packageDetails.JsonText = result.ToString();
                return packageDetails;
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetArchiveResponse():HttpRequestException", ex);
                if (!ex.Message.Contains("404") && packageType == "source")
                {
                    packageDetails.IsRetryRequired = true;
                    Logger.Debug($"GetArchiveResponse:File Name : {packageDetails.Name}, Added for Retry.");
                }
            }
            return packageDetails;
        }

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
                Logger.Debug($"GetSourceURLFromJsonText():", ex);
            }
            catch (IOException ex)
            {
                Logger.Debug($"GetSourceURLFromJsonText():", ex);
            }
            return sourceURLDetails;
        }

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
                Logger.Debug($"GetSourceURLFromJsonTextForBinaryType():", ex);
            }
            catch (IOException ex)
            {
                Logger.Debug($"GetSourceURLFromJsonTextForBinaryType():", ex);
            }

            return sourceURLDetails;
        }

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

        private async Task<DebianPackage> RetryToGetSourceURlDetailsAsync(string name, string version)
        {
            Logger.Debug($"Retry for.. {name}-{version}");
            await Task.Delay(2000);
            DebianPackage sourceDetails = await GetSourceUrl(name, version);

            if (!string.IsNullOrEmpty(sourceDetails.SourceUrl))
            {
                Logger.Debug($"Retry Success for.. {name}-{version}");
            }
            else
            {
                Logger.Debug($"Source package not found for {name}-{version}");
                Logger.Debug($"Retry Failure for.. {name}-{version}");
            }
            return sourceDetails;
        }

        /// <summary>
        /// Gets the Source Url For Python Package
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componenVersion"></param>
        /// <param name="isDebugMode"></param>
        /// <returns>string</returns>
        public async Task<string> GetSourceUrlForPythonPackage(string componentName, string componenVersion)
        {
            Logger.Debug($"URLHelper.GetSourceUrlForPythonPackage():Start");
            string name = componentName.ToLowerInvariant();
            string version = componenVersion.ToLowerInvariant();
            var response = await GetResponseFromPyPiOrg(name, version);
            string sourceURL = GetSourceURLFromPyPiResponse(response);
            return sourceURL;
        }

        private async Task<string> GetResponseFromPyPiOrg(string componentName, string componenVersion)
        {
            string URL;
            const string result = "";
            try
            {
                URL = $"{CommonAppSettings.PyPiURL}{componentName}" +
                    $"{Dataconstant.ForwardSlash}{componenVersion}{Dataconstant.ForwardSlash}json";

                var response = await httpClient.GetStringAsync(URL);
                return response.ToString();
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetResponseFromPyPiOrg():HttpRequestException", ex);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"GetResponseFromPyPiOrg():TaskCanceledException", ex);
            }
            return result;
        }

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

                    if (!string.IsNullOrEmpty(url.ToString()) && url.ToString().EndsWith(FileConstant.TargzFileExtension))
                    {
                        SourceURL = url.ToString();
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Logger.Debug($"GetSourceURLFromPyPiResponse():", ex);
            }
            catch (IOException ex)
            {
                Logger.Debug($"GetSourceURLFromPyPiResponse():", ex);
            }
            return SourceURL;
        }

        public static async Task<string> DownloadFileAsync(Uri uri, string downloadFilePath)
        {
            string downloadedPath = string.Empty;
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(uri, downloadFilePath);
                }
                downloadedPath = downloadFilePath;
                Logger.Debug($"DownloadFileFromSnapshotorgAsync:File Name : {Path.GetFileName(downloadFilePath)} ,Downloaded Successfully!!");
            }
            catch (WebException webex)
            {
                Logger.Debug($"DownloadFileFromSnapshotorgAsync:File Name : {Path.GetFileName(downloadFilePath)},Error {webex}");
                //Waiting for server to up..
                await Task.Delay(4000);
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        await webClient.DownloadFileTaskAsync(uri, downloadFilePath);
                    }
                    downloadedPath = downloadFilePath;
                    Logger.Debug($"DownloadFileFromSnapshotorgAsync:File Name : {Path.GetFileName(downloadFilePath)},Success in retry!!");
                }
                catch (WebException)
                {
                    Logger.Debug($"DownloadFileFromSnapshotorgAsync:File Name : {Path.GetFileName(downloadFilePath)},Error in retry!!");
                }
            }
            return downloadedPath;
        }

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

    }
}
