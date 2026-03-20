// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using NUnit.Framework;
using System;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    internal class SettingsManagerTests
    {
        private SettingsManager _settingsManager;
        public SettingsManagerTests()
        {
            _settingsManager = new SettingsManager();
        }

        [SetUp]
        public void Setup()
        {
            _settingsManager = new SettingsManager();
        }

        [Test]
        public void ReadConfiguration_WhenArgsIsNull_ShouldThrowInvalidDataException()
        {
            // Arrange
            string[] args = null;
            string jsonSettingsFileName = "settings.json";
            EnvironmentHelper helper = new EnvironmentHelper();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _settingsManager.ReadConfiguration<object>(args, jsonSettingsFileName, helper));
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugIsTrue_ShouldReturnTrue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("System.Debug", "true");

            // Act
            bool result = SettingsManager.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugIsFalse_ShouldReturnFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("System.Debug", "false");

            // Act
            bool result = SettingsManager.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugIsNotSet_ShouldReturnFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("System.Debug", null);

            // Act
            bool result = SettingsManager.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.That(result, Is.False);
        }
        [Test]
        public void DisplayHelp_WhenFileExists_ShouldOutputFileContent()
        {
            // Arrange
            string testFilePath = "CLIUsageNpkg.txt";
            string expectedContent = "This is a test content for CLI usage.";
            File.WriteAllText(testFilePath, expectedContent);

            using StringWriter consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            SettingsManager.DisplayHelp();

            // Assert
            string actualOutput = consoleOutput.ToString().Trim();
            Assert.That(actualOutput, Is.EqualTo(expectedContent));

            // Cleanup
            File.Delete(testFilePath);
        }

        [Test]
        public void DisplayHelp_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            string testFilePath = "CLIUsageNpkg.txt";
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => SettingsManager.DisplayHelp());
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenIdentifierWithBaseParamsOnly_CompletesWithoutError()
        {
            // Arrange - SW360=null, Jfrog=null, SBOMSignVerify=false
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { InputFolder = tempPath, OutputFolder = tempPath },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig { SBOMSignVerify = false }
            };
            appSettings.ProjectType = "NPM";

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifier");

            // Assert
            Assert.That(appSettings.ProjectType, Is.EqualTo("NPM"));
            Assert.That(appSettings.SW360, Is.Null);
            Assert.That(appSettings.Jfrog, Is.Null);
            Assert.That(appSettings.SbomSigning.SBOMSignVerify, Is.False);
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenIdentifierWithSW360_CompletesWithoutError()
        {
            // Arrange - SW360 parameters are added to the required list
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { InputFolder = tempPath, OutputFolder = tempPath },
                SW360 = new SW360 { ProjectID = "proj-001", Token = "token-001", URL = "https://sw360.example.com" },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig { SBOMSignVerify = false }
            };
            appSettings.ProjectType = "NPM";

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifier");

            // Assert
            Assert.That(appSettings.SW360, Is.Not.Null);
            Assert.That(appSettings.SW360.ProjectID, Is.EqualTo("proj-001"));
            Assert.That(appSettings.SW360.Token, Is.EqualTo("token-001"));
            Assert.That(appSettings.SW360.URL, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenIdentifierWithJfrog_NonAlpineProjectType_CompletesWithoutError()
        {
            // Arrange - Jfrog set and ProjectType != "ALPINE" so InternalRepos is also required
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { InputFolder = tempPath, OutputFolder = tempPath },
                Jfrog = new Jfrog { Token = "jfrog-token", URL = "https://jfrog.example.com" },
                Npm = new LCT.Common.Model.Config
                {
                    Artifactory = new LCT.Common.Model.Artifactory { InternalRepos = new[] { "internal-repo" } }
                },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig { SBOMSignVerify = false }
            };
            appSettings.ProjectType = "NPM";

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifier");

            // Assert
            Assert.That(appSettings.Jfrog, Is.Not.Null);
            Assert.That(appSettings.Jfrog.Token, Is.EqualTo("jfrog-token"));
            Assert.That(appSettings.Jfrog.URL, Is.EqualTo("https://jfrog.example.com"));
            Assert.That(appSettings.Npm.Artifactory.InternalRepos, Has.Length.EqualTo(1));
            Assert.That(appSettings.Npm.Artifactory.InternalRepos[0], Is.EqualTo("internal-repo"));
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenIdentifierWithJfrog_AlpineProjectType_CompletesWithoutError()
        {
            // Arrange - ProjectType == "ALPINE" so InternalRepos is NOT added to required params
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { InputFolder = tempPath, OutputFolder = tempPath },
                Jfrog = new Jfrog { Token = "jfrog-token", URL = "https://jfrog.example.com" },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig { SBOMSignVerify = false }
            };
            appSettings.ProjectType = "ALPINE";

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifier");

            // Assert
            Assert.That(appSettings.ProjectType, Is.EqualTo("ALPINE").IgnoreCase);
            Assert.That(appSettings.Jfrog, Is.Not.Null);
            Assert.That(appSettings.Alpine, Is.Null);
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenIdentifierWithSbomSigningEnabled_CompletesWithoutError()
        {
            // Arrange - SBOMSignVerify=true adds 5 signing parameters to the required list
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { InputFolder = tempPath, OutputFolder = tempPath },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig
                {
                    SBOMSignVerify = true,
                    KeyVaultURI = "https://myvault.vault.azure.net/",
                    CertificateName = "my-cert",
                    ClientId = "client-001",
                    ClientSecret = "secret-001",
                    TenantId = "tenant-001"
                }
            };
            appSettings.ProjectType = "NPM";

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifier");

            // Assert
            Assert.That(appSettings.SbomSigning.SBOMSignVerify, Is.True);
            Assert.That(appSettings.SbomSigning.KeyVaultURI, Is.EqualTo("https://myvault.vault.azure.net/"));
            Assert.That(appSettings.SbomSigning.CertificateName, Is.EqualTo("my-cert"));
            Assert.That(appSettings.SbomSigning.ClientId, Is.EqualTo("client-001"));
            Assert.That(appSettings.SbomSigning.TenantId, Is.EqualTo("tenant-001"));
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenCreator_CompletesWithoutError()
        {
            // Arrange - Creator branch requires SW360 params and Directory.OutputFolder
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { OutputFolder = tempPath },
                SW360 = new SW360 { ProjectID = "proj-001", Token = "token-001", URL = "https://sw360.example.com" },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig { SBOMSignVerify = false }
            };

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Creator");

            // Assert
            Assert.That(appSettings.SW360.ProjectID, Is.EqualTo("proj-001"));
            Assert.That(appSettings.SW360.Token, Is.EqualTo("token-001"));
            Assert.That(appSettings.SW360.URL, Is.Not.Null.And.Not.Empty);
            Assert.That(appSettings.Directory.OutputFolder, Is.EqualTo(tempPath));
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenUploader_CompletesWithoutError()
        {
            // Arrange - Uploader branch (else) requires Jfrog params and Directory.OutputFolder
            string tempPath = Path.GetTempPath();
            var appSettings = new CommonAppSettings
            {
                Directory = new Directory { OutputFolder = tempPath },
                Jfrog = new Jfrog { Token = "jfrog-token", URL = "https://jfrog.example.com" },
                SbomSigning = new LCT.Common.Model.SbomSigningConfig { SBOMSignVerify = false }
            };

            // Act
            _settingsManager.CheckRequiredArgsToRun(appSettings, "Uploader");

            // Assert
            Assert.That(appSettings.Jfrog.URL, Is.EqualTo("https://jfrog.example.com"));
            Assert.That(appSettings.Jfrog.Token, Is.EqualTo("jfrog-token"));
            Assert.That(appSettings.Directory.OutputFolder, Is.EqualTo(tempPath));
        }
    }
}