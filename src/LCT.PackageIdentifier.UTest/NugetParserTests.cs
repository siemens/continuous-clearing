// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using LCT.Common;
using LCT.Common.Model;
using LCT.PackageIdentifier;
using LCT.Services.Interface;
using LCT.PackageIdentifier.Interface;
using Moq;
using LCT.APICommunications.Model.AQL;
using CycloneDX.Models;
using System.Threading.Tasks;
using System.Linq;
using LCT.Common.Constants;
using LCT.Common.Interface;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class NugetParserTests
    {
        private Mock<IBomHelper> _mockBomHelper;
        private NugetProcessor _nugetProcessor;
        private ICycloneDXBomParser _cycloneDXBomParser;
        [SetUp]
        public void Setup()
        {
            _mockBomHelper = new Mock<IBomHelper>();
            _cycloneDXBomParser = new Mock<ICycloneDXBomParser>().Object;
            _nugetProcessor = new NugetProcessor(_cycloneDXBomParser);
        }

        [Test]
        public void GetJfrogRepoPath_WhenPathIsEmpty_ReturnsRepoAndName()
        {
            // Arrange
            var aqlResult = new AqlResult
            {
                Repo = "my-repo",
                Name = "my-package",
                Path = ""
            };
            var nugetProcessor = new NugetProcessor(_cycloneDXBomParser);

            // Act
            var result = nugetProcessor.GetJfrogRepoPath(aqlResult);

            // Assert
            Assert.AreEqual("my-repo/my-package", result);
        }

        [Test]
        public void GetJfrogRepoPath_WhenPathIsDot_ReturnsRepoAndName()
        {
            // Arrange
            var aqlResult = new AqlResult
            {
                Repo = "my-repo",
                Name = "my-package",
                Path = "."
            };
            var nugetProcessor = new NugetProcessor(_cycloneDXBomParser);

            // Act
            var result = nugetProcessor.GetJfrogRepoPath(aqlResult);

            // Assert
            Assert.AreEqual("my-repo/my-package", result);
        }

        [Test]
        public void GetJfrogRepoPath_WhenPathIsNotEmpty_ReturnsRepoPathAndName()
        {
            // Arrange
            var aqlResult = new AqlResult
            {
                Repo = "my-repo",
                Name = "my-package",
                Path = "my-folder"
            };
            var nugetProcessor = new NugetProcessor(_cycloneDXBomParser);

            // Act
            var result = nugetProcessor.GetJfrogRepoPath(aqlResult);

            // Assert
            Assert.AreEqual("my-repo/my-folder/my-package", result);
        }

        [Test]
        public void GetJfrogArtifactoryRepoDetials_WhenAqlResultListIsEmpty_ShouldReturnEmptyAqlResult()
        {
            // Arrange
            List<AqlResult> aqlResultList = new List<AqlResult>();
            Component component = new Component();
            string jfrogRepoPath;

            // Act
            var result = _nugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Not Found in JFrogRepo", result.Repo);
            Assert.AreEqual(string.Empty, jfrogRepoPath);
        }

        [Test]
        public void GetJfrogArtifactoryRepoDetials_WhenAqlResultListContainsMatchingComponentName_ShouldReturnAqlResultWithMatchingRepo()
        {
            // Arrange
            List<AqlResult> aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "Component-1.0.0.nupkg", Repo = "Repo1",Path = "path/to" },
                new AqlResult { Name = "Component-2.0.0.nupkg", Repo = "Repo2" , Path = "path/to"}
            };
            Component component = new Component { Name = "Component", Version = "1.0.0" };
            string jfrogRepoPath;

            // Act
            var result = _nugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Repo1", result.Repo);
            Assert.AreEqual("Repo1/path/to/Component-1.0.0.nupkg", jfrogRepoPath);
        }

        [Test]
        public void GetJfrogArtifactoryRepoDetials_WhenAqlResultListDoesNotContainMatchingComponentName_ShouldReturnAqlResultWithNotFoundInRepo()
        {
            // Arrange
            List<AqlResult> aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "Component-1.0.0.nupkg", Repo = "Repo1" },
                new AqlResult { Name = "Component-2.0.0.nupkg", Repo = "Repo2" }
            };
            Component component = new Component { Name = "Component", Version = "3.0.0" };
            string jfrogRepoPath;

            // Act
            var result = _nugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Not Found in JFrogRepo", result.Repo);
            Assert.AreEqual(string.Empty, jfrogRepoPath);
        }

        [Test]
        public void GetJfrogArtifactoryRepoDetials_WhenAqlResultListContainsFullNameVersion_ShouldReturnAqlResultWithMatchingRepo()
        {
            // Arrange
            List<AqlResult> aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "Component-1.0.0.nupkg", Repo = "Repo1", Path="path/to" },
                new AqlResult { Name = "Component-2.0.0.nupkg", Repo = "Repo2", Path= "path/to" }
            };
            Component component = new Component { Name = "Component", Version = "1.0.0" };
            _mockBomHelper.Setup(x => x.GetFullNameOfComponent(component)).Returns("Component");
            string jfrogRepoPath;

            // Act
            var result = _nugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Repo1", result.Repo);
            Assert.AreEqual("Repo1/path/to/Component-1.0.0.nupkg", jfrogRepoPath);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldAddSiemensDirectProperty_WhenDirectDependencyExists()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0"
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0"
                    }
                }
            };

            var nugetDirectDependencies = new List<string>
            {
                "Component1:1.0.0",
                "Component2:2.0.0"
            };

            NugetDevDependencyParser.NugetDirectDependencies = nugetDirectDependencies;

            var expectedBom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0",
                        Properties = new List<Property>
                        {
                            new Property
                            {
                                Name = Dataconstant.Cdx_SiemensDirect,
                                Value = "true"
                            }
                        }
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0",
                        Properties = new List<Property>
                        {
                            new Property
                            {
                                Name = Dataconstant.Cdx_SiemensDirect,
                                Value = "true"
                            }
                        }
                    }
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            var nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);

            // Act
            nugetProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual(expectedBom.Components.Count, bom.Components.Count);
            Assert.AreEqual(Dataconstant.Cdx_SiemensDirect, bom.Components[0].Properties[0].Name);
            Assert.AreEqual("true", bom.Components[0].Properties[0].Value);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldNotAddSiemensDirectProperty_WhenDirectDependencyDoesNotExist()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0"
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0"
                    }
                }
            };

            var nugetDirectDependencies = new List<string>
            {
                "Component3:3.0.0",
                "Component4:4.0.0"
            };

            NugetDevDependencyParser.NugetDirectDependencies = nugetDirectDependencies;

            var expectedBom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0"
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0"
                    }
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            var nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);

            // Act
            nugetProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            // Assert
            Assert.AreEqual(expectedBom.Components.Count, bom.Components.Count);
            Assert.AreEqual(Dataconstant.Cdx_SiemensDirect, bom.Components[0].Properties[0].Name);
            Assert.AreEqual("false", bom.Components[0].Properties[0].Value);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldAddSiemensDirectProperty_WhenPropertyDoesNotExist()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0"
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0",
                        Properties = new List<Property>
                        {
                            new Property
                            {
                                Name = "SomeProperty",
                                Value = "SomeValue"
                            }
                        }
                    }
                }
            };

            var nugetDirectDependencies = new List<string>
            {
                "Component1:1.0.0",
                "Component2:2.0.0"
            };

            NugetDevDependencyParser.NugetDirectDependencies = nugetDirectDependencies;

            var expectedBom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0",
                        Properties = new List<Property>
                        {
                            new Property
                            {
                                Name = Dataconstant.Cdx_SiemensDirect,
                                Value = "true"
                            }
                        }
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0",
                        Properties = new List<Property>
                        {
                            new Property
                            {
                                Name = "SomeProperty",
                                Value = "SomeValue"
                            },
                            new Property
                            {
                                Name = Dataconstant.Cdx_SiemensDirect,
                                Value = "true"
                            }
                        }
                    }
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            var nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);

            // Act
            nugetProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual(expectedBom.Components.Count, bom.Components.Count);
            Assert.AreEqual(Dataconstant.Cdx_SiemensDirect, bom.Components[0].Properties[0].Name);
            Assert.AreEqual("true", bom.Components[0].Properties[0].Value);
        }

        [TestCase]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectednoofcomponents = 7;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "packages.config"));

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Nuget = new Config(),
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            List<NugetPackage> listofcomponents = NugetProcessor.ParsePackageConfig(packagefilepath, appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Count), "Checks for no of components");

        }
        [TestCase]
        public void InputFileIdentifaction_GivenARootPath_ReturnsSuccess()
        {
            //Arrange
            int fileCount = 2;
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string folderfilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            //Act
            List<string> allFoundConfigFiles = FolderScanner.FileScanner(folderfilepath, config);


            //Assert
            Assert.That(fileCount, Is.EqualTo(allFoundConfigFiles.Count), "Checks for total inout files found");

        }
        [TestCase]
        public void InputFileIdentifaction_GivenIncludeFileAsNull_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = null;
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string folderfilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            //Act & Assert
            Assert.Throws(typeof(ArgumentNullException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void InputFileIdentifaction_GivenInputFileAsNull_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string folderfilepath = "";


            //Act & Assert
            Assert.Throws(typeof(ArgumentException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void InputFileIdentifaction_GivenInvalidInputFile_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string folderfilepath = Path.GetFullPath(Path.Combine("..", "PackageIdentifierUTTestFiles"));

            //Act & Assert
            Assert.Throws(typeof(DirectoryNotFoundException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void IsDevDependent_GivenListOfDevComponents_ReturnsSuccess()
        {
            //Arrange
            List<ReferenceDetails> referenceDetails = new List<ReferenceDetails>()
            {
             new ReferenceDetails() { Library = "SCL.Library", Version = "3.1.2", Private = true } };

            //Act
            bool actual = NugetProcessor.IsDevDependent(referenceDetails, "SCL.Library", "3.1.2");

            //Assert
            Assert.That(true, Is.EqualTo(actual), "Component is dev dependent");
        }

        [TestCase]
        public void Parsecsproj_GivenXMLFile_ReturnsSuccess()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string csprojfilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Excludes = null;
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings commonAppSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Nuget = new Config() { Exclude = Excludes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = csprojfilepath
                }
            };
            int devDependent = 0;

            //Act
            List<ReferenceDetails> referenceDetails = NugetProcessor.Parsecsproj(commonAppSettings);
            foreach (var item in referenceDetails)
            {
                if (item.Private)
                {
                    devDependent++;
                }
            }

            //Assert
            Assert.That(1, Is.EqualTo(devDependent), "Checks for total dev dependent components found");
        }

        [TestCase]
        public void RemoveExcludedComponents_ReturnsUpdatedBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string csprojfilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Excludes = null;

            Bom bom = new Bom();
            bom.Components = new List<Component>();

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings commonAppSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Nuget = new Config() { Exclude = Excludes },
                SW360 = new SW360() { ExcludeComponents = new List<string>() },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = csprojfilepath
                }
            };

            //Act
            Bom updatedBom = NugetProcessor.RemoveExcludedComponents(commonAppSettings, bom);

            //Assert
            Assert.AreEqual(0, updatedBom.Components.Count, "Zero component exculded");

        }

        [Test]
        public async Task IdentificationOfInternalComponents_Nuget_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo1" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "animations-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.IdentificationOfInternalComponents(
                component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_Nuget_ReturnsComponentData2_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "animations-common_license-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData3_Successfully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "common",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"

            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.IdentificationOfInternalComponents(
                componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "common",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NUGET",
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_Nuget_ReturnsWithData2_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NUGET",
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsRepoName_SuccessFully()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NUGET",
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

            Assert.That(reponameActual, Is.EqualTo(aqlResult.Repo));
        }
        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsRepoName_ReturnsFailure()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NUGET",
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animation-test-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

            Assert.That("Not Found in JFrogRepo", Is.EqualTo(reponameActual));
        }
        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsRepoName_ReturnsSuccess()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NUGET",
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animations-common.1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;


            Assert.That("internalrepo1", Is.EqualTo(reponameActual));
        }
        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsNotFound_ReturnsFailure()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NUGET",
                SW360 = new SW360(),
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animation-common.1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

            Assert.That("Not Found in JFrogRepo", Is.EqualTo(reponameActual));
        }

        [TestCase]
        public void ParseProjectAssetFile_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "project.assets.json" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Nuget = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new NugetProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");

        }

        [TestCase]
        public void ParseProjectAssetFile_GivenAInputFilePath_ReturnDevDependentComp()
        {
            //Arrange
            string IsDev = "true";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "project.assets.json" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Nuget = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();


            //Act
            Bom listofcomponents = new NugetProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);
            var IsDevDependency = 
                listofcomponents.Components.Find(a => a.Name == "SonarAnalyzer.CSharp")
                .Properties[0].Value;

            //Assert
            Assert.That(IsDev, Is.EqualTo(IsDevDependency), "Checks if Dev Dependency Component or not");

        }
    }
}
