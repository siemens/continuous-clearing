// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Constants;
using LCT.SW360PackageCreator.Interfaces;
using NUnit.Framework;

namespace LCT.SW360PackageCreator.UTest
{
    /// <summary>
    /// The Repository Test Class
    /// </summary>
    [TestFixture]
    public class RepositoryTest
    {
        [Test]
        [TestCase("@angular-devkit/core", "https://github.com/angular/angular-cli.git")]
        public void IdentifyRepoURLForGit_ForGivenPackageNameAndUrl_ReturnsValidRepoUrl(string packageName, string url)
        {
            // Arrange
            IRepository repository = new Repository();

            // Act
            string identifiedRepoUrl = repository.IdentifyRepoURLForGit(url, packageName);

            // Assert
            Assert.That(identifiedRepoUrl, Is.EqualTo("https://github.com/angular/angular-cli/"));
        }

        [Test]
        [TestCase("@angular-devkit/core", "")]
        public void IdentifyRepoURLForGit_ForGivenPackageNameAndEmptyUrl_ReturnsEmptyString(string packageName, string url)
        {
            // Arrange
            IRepository repository = new Repository();

            // Act
            string identifiedRepoUrl = repository.IdentifyRepoURLForGit(url, packageName);

            // Assert
            Assert.That(identifiedRepoUrl, Is.EqualTo(""));
        }

        [Test]
        [TestCase("https://github.com/angular/angular-cli/", "@angular-devkit/core", "0.901.3")]
        public void FormGitCloneUrl_ForValidPackageandUrl_ReturnsValidGitCloneUrl(
            string url, string packageName, string version)
        {
            // Arrange
            IRepository repository = new Repository();

            // Act
            string gitCloneUrl = repository.FormGitCloneUrl(url, packageName, version);

            // Assert
            Assert.That(gitCloneUrl, Is.EqualTo("https://github.com/angular/angular-cli.git"));
        }

        [Test]
        [TestCase("", "@angular-devkit/core", "0.901.3")]
        [TestCase(Dataconstant.SourceUrlNotFound, "@angular-devkit/core", "0.901.3")]
        public void FormGitCloneUrl_ForValidPackageAndEmptyUrl_ReturnsEmptyGitCloneUrl(
            string url, string packageName, string version)
        {
            // Arrange
            IRepository repository = new Repository();

            // Act
            string gitCloneUrl = repository.FormGitCloneUrl(url, packageName, version);

            // Assert
            Assert.That(gitCloneUrl, Is.EqualTo(Dataconstant.DownloadUrlNotFound));
        }
    }
}
