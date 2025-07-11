// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;
using System.IO;
using LCT.PackageIdentifier;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class PemSignatureVerifierTests
    {
        private string tempDocumentPath;
        private string tempSignaturePath;
        private string tempPemPath;

        [SetUp]
        public void SetUp()
        {
            tempDocumentPath = Path.GetTempFileName();
            tempSignaturePath = Path.GetTempFileName();
            tempPemPath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(tempDocumentPath);
            File.Delete(tempSignaturePath);
            File.Delete(tempPemPath);
        }

        [Test]
        public void ValidatePem_WithInvalidPem_ReturnsFalse()
        {
            File.WriteAllText(tempDocumentPath, "test data");
            File.WriteAllText(tempSignaturePath, "invalid signature");
            File.WriteAllText(tempPemPath, "not a valid pem");

            var result = PemSignatureVerifier.ValidatePem(tempDocumentPath, tempSignaturePath, tempPemPath);
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidatePem_WithEmptyFiles_ReturnsFalse()
        {
            // All files are empty
            var result = PemSignatureVerifier.ValidatePem(tempDocumentPath, tempSignaturePath, tempPemPath);
            Assert.IsFalse(result);
        }

        [Test]
        public void ExtractBase64FromPem_WithHeaderAndFooter_ReturnsBase64()
        {
            string pem = "-----BEGIN PUBLIC KEY-----\nBASE64DATA\n-----END PUBLIC KEY-----";
            var result = typeof(PemSignatureVerifier)
                .GetMethod("ExtractBase64FromPem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { pem });
            Assert.AreEqual("BASE64DATA", result);
        }

        [Test]
        public void ExtractBase64FromPem_WithoutHeaderFooter_ReturnsInput()
        {
            string pem = "JUSTBASE64DATA";
            var result = typeof(PemSignatureVerifier)
                .GetMethod("ExtractBase64FromPem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { pem });
            Assert.AreEqual("JUSTBASE64DATA", result);
        }

        [Test]
        public void TryVerifyEcdsa_WithInvalidKey_ReturnsFalse()
        {
            var method = typeof(PemSignatureVerifier).GetMethod("TryVerifyEcdsa", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { new byte[1], new byte[1], new byte[1] });
            Assert.IsFalse(result);
        }

        [Test]
        public void TryVerifyRsa_WithInvalidKey_ReturnsFalse()
        {
            var method = typeof(PemSignatureVerifier).GetMethod("TryVerifyRsa", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { new byte[1], new byte[1], new byte[1] });
            Assert.IsFalse(result);
        }

        [Test]
        public void TryVerifyDsa_WithInvalidKey_ReturnsFalse()
        {
            var method = typeof(PemSignatureVerifier).GetMethod("TryVerifyDsa", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { new byte[1], new byte[1], new byte[1] });
            Assert.IsFalse(result);
        }
    }
}