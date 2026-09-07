// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Model;
using SIT.Create.Interfaces;
using SIT.Create.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Create
{
    /// <summary>
    /// Resolves source packages through the index files ("Release", "Sources", "Packages") of APT archives.
    /// Those index files are part of the Debian repository format, so the same code works for the official
    /// Debian archives as well as for any vendor or mirror archive an image installs its packages from.
    /// </summary>
    public class AptRepositoryService : IAptRepositoryService, IDisposable
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string SourceArchitecture = "source";
        private const string DefaultComponent = "main";
        private const long MaxDownloadSizeInBytes = 512L * 1024 * 1024;

        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, Task<AptReleaseInfo>> _releaseCache = new();
        private readonly ConcurrentDictionary<string, Task<AptSourceIndex>> _sourceIndexCache = new();
        private readonly ConcurrentDictionary<string, Task<Dictionary<string, List<AptBinaryEntry>>>> _binaryIndexCache = new();
        private bool _disposed;

        public AptRepositoryService() : this(new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
        {
        }

        public AptRepositoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Dataconstant.UserAgent);
        }

        /// <inheritdoc/>
        public async Task<AptSourceResolution> ResolveSourcePackageAsync(IReadOnlyList<AptRepository> repositories,
                                                                        string binaryName,
                                                                        string binaryVersion,
                                                                        IReadOnlyList<string> suiteCandidates)
        {
            if (repositories == null || repositories.Count == 0 || string.IsNullOrEmpty(binaryName))
            {
                return null;
            }

            List<AptIndexContext> contexts = await GetIndexContextsAsync(repositories, suiteCandidates);

            // The source index of an archive is authoritative, it names the pool folder and every file that
            // belongs to the source package.
            foreach (AptIndexContext context in contexts)
            {
                AptSourceLocation location = await LocateInSourceIndexAsync(context, binaryName, binaryVersion);
                if (location != null)
                {
                    List<string> fileUrls = location.FileNames
                        .Select(fileName => BuildFileUrl(context.ArchiveRoot, location.Directory, fileName))
                        .Where(url => url != null)
                        .ToList();

                    AptSourceResolution resolution = BuildResolution(location, fileUrls);
                    if (resolution != null)
                    {
                        return resolution;
                    }
                }
            }

            // Archives that publish their sources without a complete source index still follow the pool
            // convention, so the .dsc next to the binary package tells which files the source package consists of.
            foreach (AptIndexContext context in contexts)
            {
                AptSourceLocation location = await LocateInPoolAsync(context, binaryName, binaryVersion);
                if (location != null)
                {
                    AptSourceResolution resolution = BuildResolution(location, await ResolveFileUrlsAsync(location, contexts));
                    if (resolution != null)
                    {
                        return resolution;
                    }
                }
            }

            return null;
        }

        #region source package lookup

        private async Task<AptSourceLocation> LocateInSourceIndexAsync(AptIndexContext context, string binaryName, string binaryVersion)
        {
            AptSourceIndex sourceIndex = await GetSourceIndexAsync(context);

            // A binary package usually carries the name of its source package.
            AptSourceEntry entry = FindSourceEntry(sourceIndex, binaryName, binaryVersion);
            string sourceName = binaryName;

            if (entry == null)
            {
                AptBinaryEntry binary = await FindBinaryEntryAsync(context, binaryName, binaryVersion);
                if (binary == null)
                {
                    return null;
                }

                sourceName = binary.SourceName ?? binaryName;
                entry = FindSourceEntry(sourceIndex, sourceName, binary.SourceVersion ?? binary.Version);
            }

            if (entry == null || string.IsNullOrWhiteSpace(entry.Directory))
            {
                return null;
            }

            return new AptSourceLocation
            {
                Context = context,
                Name = sourceName,
                Version = entry.Version,
                Directory = entry.Directory,
                FileNames = entry.FileNames
            };
        }

        private async Task<AptSourceLocation> LocateInPoolAsync(AptIndexContext context, string binaryName, string binaryVersion)
        {
            AptBinaryEntry binary = await FindBinaryEntryAsync(context, binaryName, binaryVersion);
            int lastSeparator = binary?.FileName?.LastIndexOf('/') ?? -1;
            if (lastSeparator <= 0)
            {
                return null;
            }

            string directory = binary.FileName.Substring(0, lastSeparator);
            string sourceName = binary.SourceName ?? binaryName;
            string sourceVersion = binary.SourceVersion ?? binary.Version;
            string dscFileName = $"{sourceName}_{StripEpoch(sourceVersion)}.dsc";
            string dscUrl = BuildFileUrl(context.ArchiveRoot, directory, dscFileName);

            List<string> fileNames = dscUrl == null ? null : await ReadFileNamesFromDscAsync(dscUrl);
            if (fileNames == null)
            {
                return null;
            }

            Logger.DebugFormat("LocateInPoolAsync(): {0} is not listed in the source index of {1}, using {2} instead.",
                sourceName, context.ArchiveRoot, dscUrl);

            fileNames.Insert(0, dscFileName);
            return new AptSourceLocation
            {
                Context = context,
                Name = sourceName,
                Version = sourceVersion,
                Directory = directory,
                FileNames = fileNames
            };
        }

        private async Task<List<string>> ReadFileNamesFromDscAsync(string dscUrl)
        {
            byte[] payload = await TryDownloadAsync(dscUrl);
            if (payload == null)
            {
                return null;
            }

            using StreamReader reader = new(new MemoryStream(payload), Encoding.UTF8);
            Dictionary<string, string> stanza = ParseStanzas(reader).FirstOrDefault(entry => entry.ContainsKey("Files"));
            return stanza == null ? null : ParseFileNames(stanza);
        }

        /// <summary>
        /// Determines the download URL of every file of a source package. Files that the archive of the source
        /// package does not serve itself are looked up in the remaining archives, which is what happens when a
        /// vendor archive ships only the packaging delta and leaves the upstream tarball to the distribution.
        /// </summary>
        private async Task<List<string>> ResolveFileUrlsAsync(AptSourceLocation location, List<AptIndexContext> contexts)
        {
            List<string> fileUrls = new();

            foreach (string fileName in location.FileNames)
            {
                string url = BuildFileUrl(location.Context.ArchiveRoot, location.Directory, fileName);
                if (url != null && await ExistsAsync(url))
                {
                    fileUrls.Add(url);
                    continue;
                }

                url = await FindFileInArchivesAsync(fileName, contexts);
                if (url != null)
                {
                    fileUrls.Add(url);
                }
                else
                {
                    Logger.WarnFormat("ResolveFileUrlsAsync(): {0} of source package {1}-{2} is not served by any configured APT repository.",
                        fileName, location.Name, location.Version);
                }
            }

            return fileUrls;
        }

        private async Task<string> FindFileInArchivesAsync(string fileName, List<AptIndexContext> contexts)
        {
            foreach (AptIndexContext context in contexts)
            {
                AptSourceIndex sourceIndex = await GetSourceIndexAsync(context);
                if (!sourceIndex.FileLocations.TryGetValue(fileName, out string directory))
                {
                    continue;
                }

                string url = BuildFileUrl(context.ArchiveRoot, directory, fileName);
                if (url != null && await ExistsAsync(url))
                {
                    return url;
                }
            }

            return null;
        }

        private static AptSourceResolution BuildResolution(AptSourceLocation location, List<string> fileUrls)
        {
            if (fileUrls.Count == 0)
            {
                return null;
            }

            Logger.DebugFormat("BuildResolution(): Source package {0}-{1} resolved to {2} file(s) in {3} ({4}).",
                location.Name, location.Version, fileUrls.Count, location.Context.ArchiveRoot, location.Context.Suite);

            return new AptSourceResolution
            {
                Name = location.Name,
                Version = location.Version,
                FileUrls = fileUrls,
                Origin = string.IsNullOrEmpty(location.Context.RepositoryName)
                    ? $"{location.Context.ArchiveRoot} {location.Context.Suite}"
                    : location.Context.RepositoryName
            };
        }

        private static AptSourceEntry FindSourceEntry(AptSourceIndex sourceIndex, string name, string version)
        {
            return sourceIndex.Packages.TryGetValue(name, out List<AptSourceEntry> entries)
                ? entries.Find(entry => VersionMatches(entry.Version, version))
                : null;
        }

        private async Task<AptBinaryEntry> FindBinaryEntryAsync(AptIndexContext context, string binaryName, string binaryVersion)
        {
            foreach (string architecture in context.Architectures)
            {
                Dictionary<string, List<AptBinaryEntry>> binaryIndex = await GetBinaryIndexAsync(context, architecture);
                if (binaryIndex.TryGetValue(binaryName, out List<AptBinaryEntry> candidates))
                {
                    AptBinaryEntry binary = candidates.Find(candidate => VersionMatches(candidate.Version, binaryVersion));
                    if (binary != null)
                    {
                        return binary;
                    }
                }
            }

            return null;
        }

        #endregion

        #region index handling

        private async Task<List<AptIndexContext>> GetIndexContextsAsync(IReadOnlyList<AptRepository> repositories,
                                                                        IReadOnlyList<string> suiteCandidates)
        {
            List<AptIndexContext> contexts = new();

            foreach (AptRepository repository in repositories)
            {
                string archiveRoot = NormalizeArchiveRoot(repository?.Uri);
                if (archiveRoot == null)
                {
                    Logger.WarnFormat("GetIndexContextsAsync(): Skipping APT repository '{0}', '{1}' is not a valid http(s) archive root.",
                        repository?.Name, repository?.Uri);
                    continue;
                }

                foreach (string suite in GetSuites(repository, suiteCandidates))
                {
                    AptReleaseInfo release = await GetReleaseAsync(archiveRoot, suite);
                    if (release == null)
                    {
                        continue;
                    }

                    string[] components = FirstNonEmpty(repository.Components, release.Components, new[] { DefaultComponent });
                    string[] architectures = FirstNonEmpty(repository.Architectures,
                        release.Architectures.Where(architecture => !architecture.Equals(SourceArchitecture, StringComparison.OrdinalIgnoreCase)).ToArray());

                    contexts.AddRange(components.Select(component => new AptIndexContext
                    {
                        RepositoryName = repository.Name,
                        ArchiveRoot = archiveRoot,
                        Suite = suite,
                        Component = component,
                        Architectures = architectures
                    }));
                }
            }

            if (contexts.Count == 0)
            {
                Logger.WarnFormat("GetIndexContextsAsync(): None of the {0} configured APT repositories provides a readable suite. " +
                    "Configure the suites of a repository when the components carry no distribution.", repositories.Count);
            }

            return contexts;
        }

        private async Task<AptReleaseInfo> GetReleaseAsync(string archiveRoot, string suite)
        {
            return await _releaseCache.GetOrAdd($"{archiveRoot}|{suite}", _ => FetchReleaseAsync(archiveRoot, suite));
        }

        private async Task<AptSourceIndex> GetSourceIndexAsync(AptIndexContext context)
        {
            return await _sourceIndexCache.GetOrAdd($"{context.ArchiveRoot}|{context.Suite}|{context.Component}",
                _ => FetchSourceIndexAsync(context));
        }

        private async Task<Dictionary<string, List<AptBinaryEntry>>> GetBinaryIndexAsync(AptIndexContext context, string architecture)
        {
            return await _binaryIndexCache.GetOrAdd($"{context.ArchiveRoot}|{context.Suite}|{context.Component}|{architecture}",
                _ => FetchBinaryIndexAsync(context, architecture));
        }

        private async Task<AptReleaseInfo> FetchReleaseAsync(string archiveRoot, string suite)
        {
            using StreamReader reader = await OpenIndexAsync($"{archiveRoot}/dists/{suite}/Release");
            if (reader == null)
            {
                Logger.DebugFormat("FetchReleaseAsync(): Suite {0} is not available in {1}.", suite, archiveRoot);
                return null;
            }

            Dictionary<string, string> stanza = ParseStanzas(reader).FirstOrDefault();
            if (stanza == null)
            {
                return null;
            }

            return new AptReleaseInfo
            {
                Components = SplitFieldValue(stanza, "Components"),
                Architectures = SplitFieldValue(stanza, "Architectures")
            };
        }

        private async Task<AptSourceIndex> FetchSourceIndexAsync(AptIndexContext context)
        {
            AptSourceIndex index = new();
            using StreamReader reader = await OpenIndexAsync($"{context.ArchiveRoot}/dists/{context.Suite}/{context.Component}/source/Sources");
            if (reader == null)
            {
                return index;
            }

            foreach (Dictionary<string, string> stanza in ParseStanzas(reader))
            {
                if (!stanza.TryGetValue("Package", out string name) || !stanza.TryGetValue("Version", out string version))
                {
                    continue;
                }

                stanza.TryGetValue("Directory", out string directory);
                AptSourceEntry entry = new()
                {
                    Version = version,
                    Directory = directory,
                    FileNames = ParseFileNames(stanza)
                };

                AddToIndex(index.Packages, name, entry);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    foreach (string fileName in entry.FileNames)
                    {
                        index.FileLocations[fileName] = directory;
                    }
                }
            }

            Logger.DebugFormat("FetchSourceIndexAsync(): Read {0} source packages from {1} {2}/{3}.",
                index.Packages.Count, context.ArchiveRoot, context.Suite, context.Component);
            return index;
        }

        private async Task<Dictionary<string, List<AptBinaryEntry>>> FetchBinaryIndexAsync(AptIndexContext context, string architecture)
        {
            Dictionary<string, List<AptBinaryEntry>> index = new(StringComparer.Ordinal);
            using StreamReader reader = await OpenIndexAsync($"{context.ArchiveRoot}/dists/{context.Suite}/{context.Component}/binary-{architecture}/Packages");
            if (reader == null)
            {
                return index;
            }

            foreach (Dictionary<string, string> stanza in ParseStanzas(reader))
            {
                if (!stanza.TryGetValue("Package", out string name) || !stanza.TryGetValue("Version", out string version))
                {
                    continue;
                }

                stanza.TryGetValue("Source", out string source);
                stanza.TryGetValue("Filename", out string fileName);
                (string sourceName, string sourceVersion) = ParseSourceField(source);
                AddToIndex(index, name, new AptBinaryEntry
                {
                    Version = version,
                    SourceName = sourceName,
                    SourceVersion = sourceVersion,
                    FileName = fileName
                });
            }

            Logger.DebugFormat("FetchBinaryIndexAsync(): Read {0} binary packages from {1} {2}/{3} ({4}).",
                index.Count, context.ArchiveRoot, context.Suite, context.Component, architecture);
            return index;
        }

        /// <summary>
        /// Downloads an index file, preferring the gzip compressed variant that every APT archive provides.
        /// </summary>
        private async Task<StreamReader> OpenIndexAsync(string indexUrl)
        {
            byte[] payload = await TryDownloadAsync($"{indexUrl}.gz");
            if (payload != null)
            {
                return new StreamReader(new GZipStream(new MemoryStream(payload), CompressionMode.Decompress), Encoding.UTF8);
            }

            payload = await TryDownloadAsync(indexUrl);
            return payload == null ? null : new StreamReader(new MemoryStream(payload), Encoding.UTF8);
        }

        private async Task<byte[]> TryDownloadAsync(string url)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                if (response.Content.Headers.ContentLength > MaxDownloadSizeInBytes)
                {
                    Logger.WarnFormat("TryDownloadAsync(): Ignoring {0}, the file exceeds {1} bytes.", url, MaxDownloadSizeInBytes);
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("TryDownloadAsync", $"MethodName:TryDownloadAsync(), Url: {url}", ex,
                    "A network error occurred while reading a file of an APT repository.");
            }
            catch (TaskCanceledException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("TryDownloadAsync", $"MethodName:TryDownloadAsync(), Url: {url}", ex,
                    "Reading a file of an APT repository timed out.");
            }

            return null;
        }

        private async Task<bool> ExistsAsync(string url)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Head, url);
                using HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                // Archives that do not implement HEAD cannot be probed, they are given the benefit of the doubt.
                return response.IsSuccessStatusCode ||
                       response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                       response.StatusCode == HttpStatusCode.NotImplemented;
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("ExistsAsync", $"MethodName:ExistsAsync(), Url: {url}", ex,
                    "A network error occurred while checking a file of an APT repository.");
            }
            catch (TaskCanceledException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("ExistsAsync", $"MethodName:ExistsAsync(), Url: {url}", ex,
                    "Checking a file of an APT repository timed out.");
            }

            return false;
        }

        #endregion

        #region parsing helpers

        /// <summary>
        /// Reads the RFC822 style stanzas an APT index file or a .dsc consists of.
        /// </summary>
        internal static IEnumerable<Dictionary<string, string>> ParseStanzas(TextReader reader)
        {
            Dictionary<string, string> stanza = new(StringComparer.OrdinalIgnoreCase);
            StringBuilder value = new();
            string field = null;

            void CloseField()
            {
                if (field != null)
                {
                    stanza[field] = value.ToString();
                }

                field = null;
                value.Clear();
            }

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    CloseField();
                    if (stanza.Count > 0)
                    {
                        yield return stanza;
                        stanza = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                if (line[0] == ' ' || line[0] == '\t')
                {
                    if (field != null)
                    {
                        value.Append('\n').Append(line.Trim());
                    }

                    continue;
                }

                CloseField();
                int separator = line.IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                field = line.Substring(0, separator);
                value.Append(line.Substring(separator + 1).Trim());
            }

            CloseField();
            if (stanza.Count > 0)
            {
                yield return stanza;
            }
        }

        /// <summary>
        /// The "Source" field is either "name" or "name (version)" and is omitted when it equals the binary package.
        /// </summary>
        internal static (string Name, string Version) ParseSourceField(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return (null, null);
            }

            int versionStart = source.IndexOf('(');
            if (versionStart < 0)
            {
                return (source.Trim(), null);
            }

            string name = source.Substring(0, versionStart).Trim();
            string version = source.Substring(versionStart + 1).TrimEnd(')', ' ').Trim();
            return (name, string.IsNullOrEmpty(version) ? null : version);
        }

        internal static List<string> ParseFileNames(Dictionary<string, string> stanza)
        {
            List<string> fileNames = new();
            if (!stanza.TryGetValue("Files", out string files))
            {
                return fileNames;
            }

            foreach (string line in files.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                // Every line of the "Files" field is "<checksum> <size> <file name>".
                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3 && !fileNames.Contains(parts[2]))
                {
                    fileNames.Add(parts[2]);
                }
            }

            return fileNames;
        }

        /// <summary>
        /// Builds the download URL of a file and makes sure it stays inside the archive root.
        /// </summary>
        internal static string BuildFileUrl(string archiveRoot, string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            string relativePath = $"{directory.Trim('/')}/{fileName.Trim()}";
            if (relativePath.Contains("..") || relativePath.Contains("//") || relativePath.Any(char.IsWhiteSpace))
            {
                return null;
            }

            string candidate = $"{archiveRoot}/{relativePath}";
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri uri) ||
                !uri.AbsoluteUri.StartsWith($"{archiveRoot}/", StringComparison.Ordinal))
            {
                return null;
            }

            return uri.AbsoluteUri;
        }

        internal static bool VersionMatches(string indexVersion, string requestedVersion)
        {
            if (string.IsNullOrEmpty(indexVersion) || string.IsNullOrEmpty(requestedVersion))
            {
                return false;
            }

            // The version of an installed package is often reported without its epoch.
            return string.Equals(indexVersion, requestedVersion, StringComparison.Ordinal) ||
                   string.Equals(StripEpoch(indexVersion), StripEpoch(requestedVersion), StringComparison.Ordinal);
        }

        internal static string StripEpoch(string version)
        {
            int separator = version.IndexOf(':');
            if (separator <= 0)
            {
                return version;
            }

            return version.Substring(0, separator).All(char.IsDigit) ? version.Substring(separator + 1) : version;
        }

        internal static string NormalizeArchiveRoot(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri) ||
                !Uri.TryCreate(uri.Trim().TrimEnd('/'), UriKind.Absolute, out Uri archiveRoot) ||
                (archiveRoot.Scheme != Uri.UriSchemeHttp && archiveRoot.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return archiveRoot.AbsoluteUri.TrimEnd('/');
        }

        private static void AddToIndex<T>(Dictionary<string, List<T>> index, string name, T entry)
        {
            if (!index.TryGetValue(name, out List<T> entries))
            {
                entries = new List<T>();
                index.Add(name, entries);
            }

            entries.Add(entry);
        }

        private static IEnumerable<string> GetSuites(AptRepository repository, IReadOnlyList<string> suiteCandidates)
        {
            return FirstNonEmpty(repository.Suites, suiteCandidates?.ToArray())
                .Where(suite => !string.IsNullOrWhiteSpace(suite) && !suite.Contains('/'))
                .Distinct(StringComparer.Ordinal);
        }

        private static string[] FirstNonEmpty(params string[][] candidates)
        {
            return Array.Find(candidates, candidate => candidate != null && candidate.Length > 0) ?? Array.Empty<string>();
        }

        private static string[] SplitFieldValue(Dictionary<string, string> stanza, string field)
        {
            return stanza.TryGetValue(field, out string value)
                ? value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();
        }

        #endregion

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

            if (disposing)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
    }
}
