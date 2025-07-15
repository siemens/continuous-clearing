// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NuGet.ProjectModel;
using NuGet.Versioning;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class NugetParserTests
    {
        private Mock<IBomHelper> _mockBomHelper;
        private NugetProcessor _nugetProcessor;
        private ICycloneDXBomParser _cycloneDXBomParser;
        private Mock<IFrameworkPackages> _frameworkPackages;
        private Mock<ICompositionBuilder> _compositionBuilder;
        private ISpdxBomParser _spdxBomParser;

        [SetUp]
        public void Setup()
        {
            _mockBomHelper = new Mock<IBomHelper>();
            _cycloneDXBomParser = new Mock<ICycloneDXBomParser>().Object;
            _frameworkPackages = new Mock<IFrameworkPackages>();
            _compositionBuilder = new Mock<ICompositionBuilder>();
            _spdxBomParser = new Mock<ISpdxBomParser>().Object;
            _nugetProcessor = new NugetProcessor(_cycloneDXBomParser, _frameworkPackages.Object, _compositionBuilder.Object,_spdxBomParser);
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

            // Act
            var result = NugetProcessor.GetJfrogRepoPath(aqlResult);

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

            // Act
            var result = NugetProcessor.GetJfrogRepoPath(aqlResult);

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

            // Act
            var result = NugetProcessor.GetJfrogRepoPath(aqlResult);

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
            var result = NugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

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
            var result = NugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

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
            var result = NugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

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
            var result = NugetProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPath);

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

            NugetDevDependencyParser.SetDirectDependencies(nugetDirectDependencies);

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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            var nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);

            // Act
            NugetProcessor.AddSiemensDirectProperty(ref bom);

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

            NugetDevDependencyParser.SetDirectDependencies(nugetDirectDependencies);

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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            var nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);

            // Act
            NugetProcessor.AddSiemensDirectProperty(ref bom);

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

            NugetDevDependencyParser.SetDirectDependencies(nugetDirectDependencies);

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
            var nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);

            // Act
            NugetProcessor.AddSiemensDirectProperty(ref bom);

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
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config(),
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
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
           
            CommonAppSettings commonAppSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Exclude = Excludes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
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
            Assert.That(2, Is.EqualTo(devDependent), "Checks for total dev dependent components found");
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
            CommonAppSettings commonAppSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Exclude = Excludes },
                SW360 = new SW360() { ExcludeComponents = new List<string>() },
                Directory = new LCT.Common.Directory()
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
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);
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
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object,_spdxBomParser);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object,_spdxBomParser);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object,_spdxBomParser);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object,_spdxBomParser);
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
           
            CommonAppSettings appSettings = new CommonAppSettings()
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
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object,_spdxBomParser).ParsePackageFile(appSettings);

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

            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();


            //Act
            Bom listofcomponents = new NugetProcessor(cycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser).ParsePackageFile(appSettings);
            var IsDevDependency =
                listofcomponents.Components.Find(a => a.Name == "SonarAnalyzer.CSharp")
                .Properties[0].Value;

            //Assert
            Assert.That(IsDev, Is.EqualTo(IsDevDependency), "Checks if Dev Dependency Component or not");

        }

        [Test]
        public void ParsePackageFile_WhenSelfContainedProject_DetectsDeploymentTypeCorrectlyAndFrameworkLogicWillNotApply()
        {
            // Arrange
            Mock<ICycloneDXBomParser> _mockCycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "project.assets.json", "Nuget-SelfContained.csproj" };
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };

            var frameworkPackages = new Dictionary<string, Dictionary<string, NuGetVersion>>
        {
            { "net6.0-Microsoft.NETCore.App", new Dictionary<string, NuGetVersion> { { "Newtonsoft.Json", NuGetVersion.Parse("13.0.3") } } }
        };

            string[] frameworkReferences = new[] {
                "Microsoft.NETCore.App"
            };

            _frameworkPackages
                .Setup(x => x.GetFrameworkPackages(It.IsAny<List<string>>()))
                .Returns(frameworkPackages);
            _frameworkPackages
                .Setup(x => x.GetFrameworkReferences(It.IsAny<LockFile>(), It.IsAny<LockFileTarget>()))
                .Returns(frameworkReferences);

            var bom = new Bom
            {
                Components = new List<Component>
            {
                new Component
                {
                    Name = "TestComponent",
                    Version = "1.0.0",
                    Properties = new List<Property>()
                }
            }
            };

            _mockCycloneDXBomParser
                .Setup(x => x.ParseCycloneDXBom(It.IsAny<string>()))
                .Returns(bom);

            // Act
            var result = _nugetProcessor.ParsePackageFile(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("false", result.Components[0].Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment)?.Value);
        }

        [Test]
        public void ParsePackageFile_WhenFrameworkPackagesAreProvided_AddsFrameworkPackagesAlsoToBom()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "project.assets.json", "Nuget.csproj" };
            string[] excludes = { "NugetSelfContainedProject" };
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Include = Includes, Exclude = excludes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };

            var frameworkPackages = new Dictionary<string, Dictionary<string, NuGetVersion>>
            {
                { "net6.0-runtime", new Dictionary<string, NuGetVersion> { { "TestComponent", NuGetVersion.Parse("1.0.0") } } }
            };

            _frameworkPackages
                .Setup(x => x.GetFrameworkPackages(It.IsAny<List<string>>()))
                .Returns(frameworkPackages);

            _compositionBuilder
                .Setup(x => x.AddCompositionsToBom(It.IsAny<Bom>(), It.IsAny<Dictionary<string, Dictionary<string, NuGetVersion>>>()))
                .Verifiable();

            // Act
            var result = _nugetProcessor.ParsePackageFile(appSettings);

            // Assert
            _frameworkPackages.Verify(x => x.GetFrameworkPackages(It.IsAny<List<string>>()), Times.Once);
            Assert.AreEqual(2, result.Components.Count);
            Assert.IsNotNull(result);
        }

        [Test]
        public void ParsePackageFile_WhenComponentIsFrameworkDependent_MarksComponentAsDevDependency()
        {
            // Arrange
            Mock<ICycloneDXBomParser> _mockCycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "project.assets.json" };
            string[] excludes = { "NugetSelfContainedProject" };
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Include = Includes, Exclude = excludes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };

            var frameworkPackages = new Dictionary<string, Dictionary<string, NuGetVersion>>
        {
            { "net6.0-Microsoft.NETCore.App", new Dictionary<string, NuGetVersion> { { "Newtonsoft.Json", NuGetVersion.Parse("13.0.3") } } }
        };

            string[] frameworkReferences = new[] {
                "Microsoft.NETCore.App"
            };

            _frameworkPackages
                .Setup(x => x.GetFrameworkPackages(It.IsAny<List<string>>()))
                .Returns(frameworkPackages);
            _frameworkPackages
                .Setup(x => x.GetFrameworkReferences(It.IsAny<LockFile>(), It.IsAny<LockFileTarget>()))
                .Returns(frameworkReferences);

            var bom = new Bom
            {
                Components = new List<Component>
            {
                new Component
                {
                    Name = "TestComponent",
                    Version = "1.0.0",
                    Properties = new List<Property>()
                }
            }
            };

            _mockCycloneDXBomParser
                .Setup(x => x.ParseCycloneDXBom(It.IsAny<string>()))
                .Returns(bom);

            // Act
            var result = _nugetProcessor.ParsePackageFile(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("true", result.Components.First().Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment)?.Value);
        }

        [Test]
        public void ParsePackageFile_WhenNoFrameworkPackagesAreProvided_FrameworkPackagesWillBeAddedAsRequiredComponents()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "project.assets.json" };
            string[] excludes = { "NugetSelfContainedProject" };
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Nuget = new Config() { Include = Includes, Exclude = excludes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };

            _frameworkPackages
                .Setup(x => x.GetFrameworkPackages(It.IsAny<List<string>>()))
                .Returns(new Dictionary<string, Dictionary<string, NuGetVersion>>());

            // Act
            var result = _nugetProcessor.ParsePackageFile(appSettings);

            // Assert
            _frameworkPackages.Verify(x => x.GetFrameworkPackages(It.IsAny<List<string>>()), Times.Once);
            Assert.IsNotNull(result);
            Assert.AreEqual("false", result.Components.First().Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment)?.Value);
        }        

        [Test]
        public void HandleConfigFile_WhenCycloneDXHasNullDependencies_DoesNotThrowException()
        {
            // Arrange
            var filepath = "test.cdx.json";
            var appSettings = new CommonAppSettings { ProjectType = "NUGET" };
            var listComponentForBOM = new List<Component>();
            var bom = new Bom { Dependencies = new List<Dependency>() };
            var listOfTemplateBomfilePaths = new List<string>();

            var mockCycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Version = "1.0.0",Purl="TestComponent@1.0.0" }
        },
                Dependencies = new List<Dependency>()
            };
            mockCycloneDXBomParser.Setup(x => x.ParseCycloneDXBom(filepath)).Returns(testBom);

            var nugetProcessor = new NugetProcessor(mockCycloneDXBomParser.Object, _frameworkPackages.Object, _compositionBuilder.Object, _spdxBomParser);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                nugetProcessor.GetType()
                    .GetMethod("HandleConfigFile", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(nugetProcessor, new object[] { filepath, appSettings, listComponentForBOM, bom, listOfTemplateBomfilePaths });
            });

            Assert.AreEqual(0, bom.Dependencies.Count);
        }
               

    }
}