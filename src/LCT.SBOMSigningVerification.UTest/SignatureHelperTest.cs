// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Helpers;
using LCT.SBOMSigningVerification.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LCT.SBOMSigningVerification.UTest
{
    [TestFixture]
    public class SignatureHelperTest
    {
        private SignatureHelper _signatureHelper;

        [SetUp]
        public void Setup()
        {
            _signatureHelper = new SignatureHelper();
        }

        #region ExtractSignature Tests

        [Test]
        public void ExtractSignature_WithValidSignature_ReturnsSignatureObject()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""version"": ""1.0.0"",
                ""signature"": {
                    ""algorithm"": ""http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"",
                    ""value"": ""dGVzdC1zaWduYXR1cmU=""
                }
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Algorithm, Is.EqualTo("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"));
            Assert.That(result.Value, Is.EqualTo("dGVzdC1zaWduYXR1cmU="));
        }

        [Test]
        public void ExtractSignature_WithNoSignature_ReturnsNull()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""version"": ""1.0.0"",
                ""components"": []
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ExtractSignature_WithOnlySignatureValue_ReturnsSignatureObject()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""signature"": {
                    ""value"": ""dGVzdC1zaWduYXR1cmU=""
                }
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Value, Is.EqualTo("dGVzdC1zaWduYXR1cmU="));
            Assert.That(result.Algorithm, Is.Null);
        }

        [Test]
        public void ExtractSignature_WithOnlySignatureAlgorithm_ReturnsSignatureObject()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""signature"": {
                    ""algorithm"": ""RS256""
                }
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Algorithm, Is.EqualTo("RS256"));
            Assert.That(result.Value, Is.Null);
        }

        [Test]
        public void ExtractSignature_WithEmptySignature_ReturnsSignatureObject()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""signature"": {}
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Algorithm, Is.Null);
            Assert.That(result.Value, Is.Null);
        }

        [Test]
        public void ExtractSignature_WithComplexJsonStructure_ReturnsSignatureObject()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""complex-sbom"",
                ""version"": ""1.0.0"",
                ""components"": [
                    {
                        ""name"": ""component1"",
                        ""version"": ""1.0"",
                        ""licenses"": [
                            {
                                ""license"": {
                                    ""name"": ""MIT""
                                }
                            }
                        ]
                    }
                ],
                ""metadata"": {
                    ""timestamp"": ""2024-01-01T00:00:00Z"",
                    ""tools"": [
                        {
                            ""name"": ""test-tool"",
                            ""version"": ""1.0""
                        }
                    ]
                },
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""Y29tcGxleC1zaWduYXR1cmU=""
                }
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Algorithm, Is.EqualTo("RS256"));
            Assert.That(result.Value, Is.EqualTo("Y29tcGxleC1zaWduYXR1cmU="));
        }

        [Test]
        public void ExtractSignature_WithInvalidJson_ThrowsException()
        {
            // Arrange
            string invalidJson = "{ invalid json }";

            // Act & Assert
            Assert.That(() => _signatureHelper.ExtractSignature(invalidJson), Throws.Exception);
        }

        [Test]
        public void ExtractSignature_WithNullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _signatureHelper.ExtractSignature(null));
        }

        [Test]
        public void ExtractSignature_WithEmptyString_ThrowsException()
        {
            // Act & Assert
            Assert.That(() => _signatureHelper.ExtractSignature(""), Throws.Exception);
        }

        [Test]
        public void ExtractSignature_WithWhitespaceOnly_ThrowsException()
        {
            // Act & Assert
            Assert.That(() => _signatureHelper.ExtractSignature("   \t\n"), Throws.Exception);
        }

        #endregion

        #region RemoveSignature Tests

        [Test]
        public void RemoveSignature_WithSignature_RemovesSignatureProperty()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""version"": ""1.0.0"",
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""dGVzdC1zaWduYXR1cmU=""
                }
            }";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Not.Contain("signature"));
            Assert.That(result, Does.Contain("name"));
            Assert.That(result, Does.Contain("test-sbom"));
            Assert.That(result, Does.Contain("version"));
            Assert.That(result, Does.Contain("1.0.0"));

            // Verify it's valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        [Test]
        public void RemoveSignature_WithoutSignature_ReturnsUnchangedJson()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""version"": ""1.0.0"",
                ""components"": []
            }";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("name"));
            Assert.That(result, Does.Contain("test-sbom"));
            Assert.That(result, Does.Contain("version"));
            Assert.That(result, Does.Contain("1.0.0"));
            Assert.That(result, Does.Contain("components"));

            // Verify it's valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        [Test]
        public void RemoveSignature_WithComplexJsonAndSignature_RemovesOnlySignature()
        {
            // Arrange
            string sbomContent = @"{
                ""bomFormat"": ""CycloneDX"",
                ""specVersion"": ""1.4"",
                ""version"": 1,
                ""metadata"": {
                    ""timestamp"": ""2024-01-01T00:00:00Z"",
                    ""tools"": [
                        {
                            ""name"": ""test-tool"",
                            ""version"": ""1.0""
                        }
                    ]
                },
                ""components"": [
                    {
                        ""type"": ""library"",
                        ""name"": ""component1"",
                        ""version"": ""1.0.0"",
                        ""purl"": ""pkg:npm/component1@1.0.0""
                    }
                ],
                ""dependencies"": [],
                ""signature"": {
                    ""algorithm"": ""http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"",
                    ""value"": ""bXVsdGktbGluZS1zaWduYXR1cmU=""
                }
            }";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Not.Contain("signature"));
            
            // Verify other properties are preserved
            Assert.That(result, Does.Contain("bomFormat"));
            Assert.That(result, Does.Contain("CycloneDX"));
            Assert.That(result, Does.Contain("metadata"));
            Assert.That(result, Does.Contain("components"));
            Assert.That(result, Does.Contain("component1"));
            Assert.That(result, Does.Contain("dependencies"));

            // Verify it's valid JSON
            var parsedResult = JsonDocument.Parse(result);
            Assert.That(parsedResult.RootElement.TryGetProperty("signature", out _), Is.False);
            Assert.That(parsedResult.RootElement.TryGetProperty("bomFormat", out _), Is.True);
            Assert.That(parsedResult.RootElement.TryGetProperty("components", out _), Is.True);
        }

        [Test]
        public void RemoveSignature_WithMultipleSignatureLikeProperties_RemovesOnlySignatureProperty()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""dGVzdA==""
                },
                ""metadata"": {
                    ""signatureInfo"": ""This is not the signature property"",
                    ""digital_signature"": ""Also not the signature property""
                }
            }";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Not.Contain("\"signature\""));
            Assert.That(result, Does.Contain("signatureInfo"));
            Assert.That(result, Does.Contain("digital_signature"));
            Assert.That(result, Does.Contain("metadata"));

            // Verify it's valid JSON
            var parsedResult = JsonDocument.Parse(result);
            Assert.That(parsedResult.RootElement.TryGetProperty("signature", out _), Is.False);
            Assert.That(parsedResult.RootElement.GetProperty("metadata").TryGetProperty("signatureInfo", out _), Is.True);
        }

        [Test]
        public void RemoveSignature_WithEmptyObject_ReturnsEmptyObject()
        {
            // Arrange
            string sbomContent = "{}";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Trim(), Does.StartWith("{"));
            Assert.That(result.Trim(), Does.EndWith("}"));

            // Verify it's valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        [Test]
        public void RemoveSignature_WithOnlySignature_ReturnsEmptyObject()
        {
            // Arrange
            string sbomContent = @"{
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""dGVzdA==""
                }
            }";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Not.Contain("signature"));

            // Verify it's valid JSON and essentially empty
            var parsedResult = JsonDocument.Parse(result);
            var hasAnyProperties = false;
            foreach (var property in parsedResult.RootElement.EnumerateObject())
            {
                hasAnyProperties = true;
                break;
            }
            Assert.That(hasAnyProperties, Is.False);
        }

        [Test]
        public void RemoveSignature_WithInvalidJson_ThrowsJsonException()
        {
            // Arrange
            string invalidJson = "{ invalid: json }";

            // Act & Assert
            // JsonDocument.Parse can throw various JSON-related exceptions
            Assert.That(() => _signatureHelper.RemoveSignature(invalidJson), Throws.Exception);
        }

        [Test]
        public void RemoveSignature_WithNullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _signatureHelper.RemoveSignature(null));
        }

        [Test]
        public void RemoveSignature_WithEmptyString_ThrowsJsonException()
        {
            // Act & Assert
            // JsonDocument.Parse can throw various JSON-related exceptions for empty string
            Assert.That(() => _signatureHelper.RemoveSignature(""), Throws.Exception);
        }

        [Test]
        public void RemoveSignature_ResultIsWellFormatted_HasProperIndentation()
        {
            // Arrange
            string sbomContent = @"{""name"":""test"",""signature"":{""value"":""test""},""version"":""1.0""}";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("\n")); // Should have newlines (indentation)
            Assert.That(result, Does.Not.Contain("signature"));
            
            // Verify it's properly formatted JSON
            var parsedResult = JsonDocument.Parse(result);
            Assert.That(parsedResult.RootElement.GetProperty("name").GetString(), Is.EqualTo("test"));
            Assert.That(parsedResult.RootElement.GetProperty("version").GetString(), Is.EqualTo("1.0"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void ExtractAndRemoveSignature_RoundTrip_WorksCorrectly()
        {
            // Arrange
            string originalSbom = @"{
                ""name"": ""test-sbom"",
                ""version"": ""1.0.0"",
                ""components"": [
                    {
                        ""name"": ""component1"",
                        ""version"": ""1.0""
                    }
                ],
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""dGVzdC1zaWduYXR1cmU=""
                }
            }";

            // Act
            var extractedSignature = _signatureHelper.ExtractSignature(originalSbom);
            var cleanedSbom = _signatureHelper.RemoveSignature(originalSbom);
            var extractedFromCleaned = _signatureHelper.ExtractSignature(cleanedSbom);

            // Assert
            Assert.That(extractedSignature, Is.Not.Null);
            Assert.That(extractedSignature.Algorithm, Is.EqualTo("RS256"));
            Assert.That(extractedSignature.Value, Is.EqualTo("dGVzdC1zaWduYXR1cmU="));
            
            Assert.That(cleanedSbom, Does.Not.Contain("signature"));
            Assert.That(cleanedSbom, Does.Contain("name"));
            Assert.That(cleanedSbom, Does.Contain("components"));
            
            Assert.That(extractedFromCleaned, Is.Null);
        }

        [Test]
        public void ExtractSignature_WithLargeJson_HandlesEfficiently()
        {
            // Arrange
            var componentsList = new List<string>();
            for (int i = 1; i <= 1000; i++)
            {
                componentsList.Add($@"{{""name"":""component{i}"",""version"":""1.0.{i}""}}");
            }
            var components = string.Join(",", componentsList);
            
            string largeSbom = $@"{{
                ""name"": ""large-sbom"",
                ""version"": ""1.0.0"",
                ""components"": [{components}],
                ""signature"": {{
                    ""algorithm"": ""RS256"",
                    ""value"": ""bGFyZ2Utc2lnbmF0dXJl""
                }}
            }}";

            // Act
            var result = _signatureHelper.ExtractSignature(largeSbom);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Algorithm, Is.EqualTo("RS256"));
            Assert.That(result.Value, Is.EqualTo("bGFyZ2Utc2lnbmF0dXJl"));
        }

        [Test]
        public void RemoveSignature_WithLargeJson_HandlesEfficiently()
        {
            // Arrange
            var componentsList = new List<string>();
            for (int i = 1; i <= 1000; i++)
            {
                componentsList.Add($@"{{""name"":""component{i}"",""version"":""1.0.{i}""}}");
            }
            var components = string.Join(",", componentsList);
            
            string largeSbom = $@"{{
                ""name"": ""large-sbom"",
                ""components"": [{components}],
                ""signature"": {{
                    ""algorithm"": ""RS256"",
                    ""value"": ""bGFyZ2Utc2lnbmF0dXJl""
                }}
            }}";

            // Act
            var result = _signatureHelper.RemoveSignature(largeSbom);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Not.Contain("signature"));
            Assert.That(result, Does.Contain("component1"));
            Assert.That(result, Does.Contain("component1000"));
            
            // Verify it's valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_DefaultConstructor_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new SignatureHelper());
        }

        [Test]
        public void Constructor_CreatesValidInstance()
        {
            // Act
            var helper = new SignatureHelper();

            // Assert
            Assert.That(helper, Is.Not.Null);
            Assert.That(helper, Is.InstanceOf<SignatureHelper>());
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ExtractSignature_WithNestedSignatureProperty_ExtractsCorrectOne()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""metadata"": {
                    ""tools"": [
                        {
                            ""name"": ""signing-tool"",
                            ""signature"": ""this-is-not-the-signature-we-want""
                        }
                    ]
                },
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""dGhpcy1pcy10aGUtcmlnaHQtc2lnbmF0dXJl""
                }
            }";

            // Act
            var result = _signatureHelper.ExtractSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Algorithm, Is.EqualTo("RS256"));
            Assert.That(result.Value, Is.EqualTo("dGhpcy1pcy10aGUtcmlnaHQtc2lnbmF0dXJl"));
        }

        [Test]
        public void RemoveSignature_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            string sbomContent = @"{
                ""name"": ""test-sbom"",
                ""description"": ""Special chars and unicode"",
                ""signature"": {
                    ""algorithm"": ""RS256"",
                    ""value"": ""c3BlY2lhbC1jaGFycy1zaWduYXR1cmU=""
                }
            }";

            // Act
            var result = _signatureHelper.RemoveSignature(sbomContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Not.Contain("signature"));
            Assert.That(result, Does.Contain("Special chars and unicode"));
            
            // Verify it's valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        #endregion
    }
}