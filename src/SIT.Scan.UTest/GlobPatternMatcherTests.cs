// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIT.Scan.UTest
{
    /// <summary>
    /// Validates the POSIX-style glob support exposed through <see cref="GlobPatternMatcher"/>
    /// for both the include and exclude pattern sections.
    /// </summary>
    [TestFixture]
    public class GlobPatternMatcherTests
    {
        private string _rootPath;

        [SetUp]
        public void SetUp()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), "SIT.GlobMatcherTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_rootPath);

            // Layout used by all tests:
            //   <root>/packages.config
            //   <root>/project.assets.json
            //   <root>/src/packages.config
            //   <root>/src/sub/packages.config
            //   <root>/src/sub/private.assets.json
            //   <root>/node_modules/packages.config
            //   <root>/build/output.txt
            CreateFile("packages.config");
            CreateFile("project.assets.json");
            CreateFile("src/packages.config");
            CreateFile("src/sub/packages.config");
            CreateFile("src/sub/private.assets.json");
            CreateFile("node_modules/packages.config");
            CreateFile("build/output.txt");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, recursive: true);
            }
        }

        [Test]
        public void GetMatchingFiles_RecursiveDoubleStarInclude_FindsAllNestedFiles()
        {
            var matches = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "**/packages.config" },
                excludePatterns: null);

            Assert.That(matches.Count, Is.EqualTo(4), "Three nested + node_modules packages.config files expected.");
            Assert.That(matches, Has.Some.EndsWith(Path.Combine("src", "packages.config")));
            Assert.That(matches, Has.Some.EndsWith(Path.Combine("src", "sub", "packages.config")));
        }

        [Test]
        public void GetMatchingFiles_BareIncludePattern_IsTreatedAsRecursive()
        {
            // Backward-compat: "packages.config" (no separator) should behave like "**/packages.config".
            var matches = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "packages.config" },
                excludePatterns: null);

            Assert.That(matches.Count, Is.EqualTo(4));
        }

        [Test]
        public void GetMatchingFiles_WildcardIncludePattern_FindsAssetsFiles()
        {
            var matches = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "**/p*.assets.json" },
                excludePatterns: null);

            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches, Has.Some.EndsWith("project.assets.json"));
            Assert.That(matches, Has.Some.EndsWith("private.assets.json"));
        }

        [Test]
        public void GetMatchingFiles_BareWildcardIncludePattern_BehavesSameAsRecursiveForm()
        {
            // Reproduces the user-reported scenario: include pattern "p*.assets.json" without
            // the leading "**/" should still find files at any depth thanks to normalization.
            var bare = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "p*.assets.json" },
                excludePatterns: null);

            var recursive = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "**/p*.assets.json" },
                excludePatterns: null);

            Assert.That(bare, Is.EquivalentTo(recursive));
            Assert.That(bare.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetMatchingFiles_ExcludeDirectoryGlob_RemovesNestedFiles()
        {
            var matches = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "**/packages.config" },
                new[] { "**/node_modules/**" });

            Assert.That(matches.Count, Is.EqualTo(3));
            Assert.That(matches, Has.None.Contains("node_modules"));
        }

        [Test]
        public void GetMatchingFiles_BareExcludePattern_AlsoExcludesDirectoryContents()
        {
            // Backward-compat: a bare name with no wildcards should also exclude everything
            // inside a directory with that name (legacy substring behavior).
            var matches = GlobPatternMatcher.GetMatchingFiles(
                _rootPath,
                new[] { "**/packages.config" },
                new[] { "node_modules" });

            Assert.That(matches.Count, Is.EqualTo(3));
            Assert.That(matches, Has.None.Contains("node_modules"));
        }

        [Test]
        public void GetMatchingFiles_InvalidRoot_ReturnsEmpty()
        {
            var matches = GlobPatternMatcher.GetMatchingFiles(
                Path.Combine(_rootPath, "does-not-exist"),
                new[] { "**/*" },
                excludePatterns: null);

            Assert.That(matches, Is.Empty);
        }

        [Test]
        public void IsMatch_RecursiveGlob_ReturnsTrueForNestedPath()
        {
            string filePath = Path.Combine(_rootPath, "src", "sub", "packages.config");

            Assert.IsTrue(GlobPatternMatcher.IsMatch(filePath, new[] { "**/packages.config" }));
            Assert.IsTrue(GlobPatternMatcher.IsMatch(filePath, new[] { "packages.config" }));
        }

        [Test]
        public void IsMatch_WildcardGlob_MatchesPartialFileName()
        {
            string filePath = Path.Combine(_rootPath, "src", "sub", "private.assets.json");

            Assert.IsTrue(GlobPatternMatcher.IsMatch(filePath, new[] { "**/p*.assets.json" }));
            Assert.IsTrue(GlobPatternMatcher.IsMatch(filePath, new[] { "p*.assets.json" }));
        }

        [Test]
        public void IsMatch_NoMatch_ReturnsFalse()
        {
            string filePath = Path.Combine(_rootPath, "build", "output.txt");

            Assert.IsFalse(GlobPatternMatcher.IsMatch(filePath, new[] { "**/*.json" }));
            Assert.IsFalse(GlobPatternMatcher.IsMatch(filePath, new[] { "**/packages.config" }));
        }

        [Test]
        public void IsMatch_NullOrEmptyInputs_ReturnFalse()
        {
            Assert.IsFalse(GlobPatternMatcher.IsMatch(null, new[] { "**/*" }));
            Assert.IsFalse(GlobPatternMatcher.IsMatch("any.txt", null));
            Assert.IsFalse(GlobPatternMatcher.IsMatch("any.txt", new List<string>()));
            Assert.IsFalse(GlobPatternMatcher.IsMatch("any.txt", new[] { "   " }));
        }

        // The following scenarios reproduce the real-world case where a user supplies
        // "**/*Test/**" and expects directories like "TestFiles" or "ViewModel.tests" to be
        // excluded. Per the POSIX glob spec, "*Test" only matches segments that END with
        // "Test" - so those directories must be excluded with the broader "**/*Test*/**".
        [TestCase(@"D:\CAInput\Eco\domains\sip5DataModel\backend\ViewModel.tests\packages.config")]
        [TestCase(@"D:\CAInput\Newfolder\TestFiles\IntegrationTestFiles\Nuget\packages.config")]
        [TestCase(@"D:\CAInput\Newfolder\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\packages.config")]
        public void IsMatch_TrailingOnlyGlob_DoesNotMatchSegmentsThatOnlyContainTest(string filePath)
        {
            // "*Test" requires the segment to END with "Test" — none of these do, so the
            // exclusion does not fire and the file remains included.
            Assert.IsFalse(GlobPatternMatcher.IsMatch(filePath, new[] { "**/*Test/**" }));
        }

        [TestCase(@"D:\CAInput\Eco\domains\sip5DataModel\backend\ViewModel.tests\packages.config")]
        [TestCase(@"D:\CAInput\Newfolder\TestFiles\IntegrationTestFiles\Nuget\packages.config")]
        [TestCase(@"D:\CAInput\Newfolder\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\packages.config")]
        public void IsMatch_SurroundingWildcards_MatchSegmentsThatContainTest(string filePath)
        {
            // "*Test*" matches any segment CONTAINING "Test" (case-insensitive), which is the
            // correct way to express "exclude anything in a test-related folder".
            Assert.IsTrue(GlobPatternMatcher.IsMatch(filePath, new[] { "**/*Test*/**" }));
        }

        [TestCase(@"D:\CAInput\Nuget\signature\plugin_byteArr_signTemplate_int_stringArr_int\packages.config")]
        [TestCase(@"D:\CAInput\Nuget\signature\plugin_byteArr_sign_byteArr_int\packages.config")]
        [TestCase(@"D:\CAInput\Nuget\signature\ServiceControlInterface\packages.config")]
        public void IsMatch_SurroundingWildcards_DoNotMatchNonTestSegments(string filePath)
        {
            Assert.IsFalse(GlobPatternMatcher.IsMatch(filePath, new[] { "**/*Test*/**" }));
        }

        private void CreateFile(string relativePath)
        {
            string fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, string.Empty);
        }
    }
}
