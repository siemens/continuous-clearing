// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIT.Scan
{
    /// <summary>
    /// Provides glob pattern matching for include and exclude file selection rules.
    /// </summary>
    /// <remarks>
    /// Supports the standard POSIX-style glob syntax described at
    /// https://en.wikipedia.org/wiki/Glob_(programming):
    /// <list type="bullet">
    ///   <item><c>*</c> matches any sequence of characters except the path separator.</item>
    ///   <item><c>**</c> matches any number of directories.</item>
    ///   <item><c>?</c> matches a single character.</item>
    ///   <item><c>[abc]</c> / <c>[a-z]</c> character classes.</item>
    /// </list>
    /// For backward compatibility, patterns that contain no path separator (for example
    /// <c>packages.config</c> or <c>p*-lock.json</c>) are automatically treated as if prefixed
    /// with <c>**/</c>, so they match anywhere in the directory tree.
    /// </remarks>
    public static class GlobPatternMatcher
    {
        private static readonly char[] GlobWildcardCharacters = { '*', '?', '[' };

        /// <summary>
        /// Scans <paramref name="rootPath"/> and returns the absolute paths of all files that
        /// match any of the <paramref name="includePatterns"/> glob patterns while not being
        /// matched by any of the <paramref name="excludePatterns"/> glob patterns.
        /// </summary>
        public static IReadOnlyList<string> GetMatchingFiles(
            string rootPath,
            IEnumerable<string> includePatterns,
            IEnumerable<string> excludePatterns)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return [];
            }

            Matcher matcher = IdentifyMatchingPatterns(includePatterns, excludePatterns);
            DirectoryInfoBase directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(rootPath));
            PatternMatchingResult result = matcher.Execute(directoryInfo);

            if (!result.HasMatches)
            {
                return [];
            }

            return [.. result.Files.Select(f => Path.GetFullPath(Path.Combine(rootPath, f.Path)))];
        }

        /// <summary>
        /// Determines whether the specified <paramref name="filePath"/> matches any of the
        /// supplied <paramref name="patterns"/>.
        /// </summary>
        /// <param name="filePath">The file path (absolute or relative) to evaluate.</param>
        /// <param name="patterns">The glob patterns to test against.</param>
        public static bool IsMatch(string filePath, IEnumerable<string> patterns)
        {
            if (string.IsNullOrEmpty(filePath) || patterns == null)
            {
                return false;
            }

            List<string> normalizedPatterns = [.. patterns
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .SelectMany(FormattingPattern)];

            if (normalizedPatterns.Count == 0)
            {
                return false;
            }

            Matcher matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            foreach (string pattern in normalizedPatterns)
            {
                matcher.AddInclude(pattern);
            }

            string candidate = IdentifyRootPath(filePath);
            return matcher.Match(candidate).HasMatches;
        }

        private static Matcher IdentifyMatchingPatterns(
            IEnumerable<string> includePatterns,
            IEnumerable<string> excludePatterns)
        {
            Matcher matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            if (includePatterns != null)
            {
                foreach (string pattern in includePatterns)
                {
                    foreach (string normalized in FormattingPattern(pattern))
                    {
                        matcher.AddInclude(normalized);
                    }
                }
            }

            if (excludePatterns != null)
            {
                foreach (string pattern in excludePatterns)
                {
                    foreach (string normalized in FormattingPattern(pattern))
                    {
                        matcher.AddExclude(normalized);
                    }
                }
            }

            return matcher;
        }

        /// <summary>
        /// Normalizes a single user-provided pattern into one or more glob patterns that the
        /// underlying <see cref="Matcher"/> can evaluate.
        /// </summary>
        /// <remarks>
        /// Patterns without a path separator are expanded to also match anywhere in the tree
        /// (and, when they have no wildcards, to match the contents of a directory with that
        /// name) so that legacy, non-glob configurations keep working.
        /// </remarks>
        internal static IEnumerable<string> FormattingPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                yield break;
            }

            string normalized = pattern.Replace('\\', '/').Trim();

            if (normalized.StartsWith("./", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2);
            }

            if (!normalized.Contains('/'))
            {
                yield return "**/" + normalized;

                // Preserve the historical "match anywhere, including contents" semantics
                // when the user supplied a plain name such as "node_modules".
                if (normalized.IndexOfAny(GlobWildcardCharacters) < 0)
                {
                    yield return "**/" + normalized + "/**";
                }
            }
            else
            {
                yield return normalized;
            }
        }

        /// <summary>
        /// Converts a file path (which may be absolute and may use either path separator)
        /// into a relative, forward-slash path suitable for <see cref="Matcher.Match(string)"/>.
        /// </summary>
        private static string IdentifyRootPath(string filePath)
        {
            string normalized = filePath.Replace('\\', '/');

            if (Path.IsPathRooted(filePath))
            {
                string root = Path.GetPathRoot(filePath)?.Replace('\\', '/') ?? string.Empty;
                if (!string.IsNullOrEmpty(root) &&
                    normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized[root.Length..];
                }
            }

            return normalized.TrimStart('/');
        }
    }
}
