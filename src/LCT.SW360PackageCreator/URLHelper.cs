// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

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
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        private bool _disposed;


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
                Logger.Debug($"GetSourceURLFromNuspecFile():", ex);
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

            return $"{Dataconstant.DebianPackage}{Dataconstant.ForwardSlash}{name}@{version}?arch=source";
        }

        private static string GetComponentExternalId(string name)
        {
            return $"{Dataconstant.DebianPackage}{Dataconstant.ForwardSlash}{name}?arch=source";
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
            string OrigFile = URLs.FirstOrDefault(item => item.Contains(FileConstant.OrigTarFileExtension) &&
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
                var sourceURL = URLs.FirstOrDefault(item => !item.Contains(FileConstant.OrigTarFileExtension) && !item.Contains(FileConstant.DSCFileExtension)
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
                    var dscFile = URLs.FirstOrDefault(item => item.Contains(FileConstant.DSCFileExtension));

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
