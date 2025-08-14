// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using File = System.IO.File;
using LCT.Common.ComplianceValidator;
using LCT.Common.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LCT.Common.Tests.ComplianceValidator
{
    [TestFixture]
    public class ComplianceCheckTests
    {
        private ComplianceCheck _complianceCheck;

        [SetUp]
        public void Setup()
        {
            _complianceCheck = new ComplianceCheck();
        }

        [Test]
        public async Task LoadSettingsAsync_ValidJsonFile_ReturnsSettingsModel()
        {
            // Arrange
            var settings = new ComplianceSettingsModel
            {
                ComplianceExceptionComponents = new List<ComplianceExceptionComponent>
                {
                    new ComplianceExceptionComponent
                    {
                        Id = "1",
                        Name = "TestComp",
                        Description = "desc",
                        Purl = new List<string> { "pkg:maven/test@1.0.0" },
                        ComplianceInstructions = new ComplianceInstructions
                        {
                            WarningMessage = "Warn",
                            Recommendation = "Rec"
                        }
                    }
                }
            };
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, JsonSerializer.Serialize(settings));

            // Act
            var result = await _complianceCheck.LoadSettingsAsync(tempFile);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ComplianceExceptionComponents);
            Assert.AreEqual("1", result.ComplianceExceptionComponents[0].Id);

            File.Delete(tempFile);
        }

        [Test]
        public void Check_NullSettingsOrData_ReturnsFalse()
        {
            Assert.IsFalse(_complianceCheck.Check(null, new List<ComparisonBomData>()));
            Assert.IsFalse(_complianceCheck.Check(new ComplianceSettingsModel(), null));
        }

        [Test]
        public void Check_DataNotListOfComparisonBomData_ReturnsFalse()
        {
            var settings = new ComplianceSettingsModel();
            Assert.IsFalse(_complianceCheck.Check(settings, new object()));
        }

        [Test]
        public void Check_NoWarnings_ReturnsTrue()
        {
            var settings = new ComplianceSettingsModel
            {
                ComplianceExceptionComponents = new List<ComplianceExceptionComponent>()
            };
            var data = new List<ComparisonBomData>();
            Assert.IsTrue(_complianceCheck.Check(settings, data));
        }

        [Test]
        public void Check_WithWarnings_ReturnsFalse()
        {
            var comp = new ComplianceExceptionComponent
            {
                Purl = new List<string> { "pkg:maven/test@1.0.0" },
                ComplianceInstructions = new ComplianceInstructions
                {
                    WarningMessage = "Test warning",
                    Recommendation = "Test recommendation"
                }
            };
            var settings = new ComplianceSettingsModel
            {
                ComplianceExceptionComponents = new List<ComplianceExceptionComponent> { comp }
            };
            var data = new List<ComparisonBomData>
            {
                new ComparisonBomData { ComponentExternalId = "pkg:maven/test@1.0.0" }
            };

            Assert.IsFalse(_complianceCheck.Check(settings, data));
            var warnings = _complianceCheck.GetResults();
            Assert.IsTrue(warnings.Count > 0);
            Assert.IsTrue(warnings[0].Contains("Test warning"));
        }

        [Test]
        public void PrintRecommendation_NullOrWhitespace_DoesNothing()
        {
            Assert.DoesNotThrow(() => _complianceCheck.PrintRecommendation(null));
            Assert.DoesNotThrow(() => _complianceCheck.PrintRecommendation(""));
            Assert.DoesNotThrow(() => _complianceCheck.PrintRecommendation("   "));
        }

        [Test]
        public void PrintWarning_NullOrWhitespace_DoesNothing()
        {
            Assert.DoesNotThrow(() => _complianceCheck.PrintWarning(null));
            Assert.DoesNotThrow(() => _complianceCheck.PrintWarning(""));
            Assert.DoesNotThrow(() => _complianceCheck.PrintWarning("   "));
        }

        [Test]
        public void GetResults_ReturnsWarningsList()
        {
            var result = _complianceCheck.GetResults();
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<List<string>>(result);
        }
    }
}

