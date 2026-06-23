// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Moq;
using NUnit.Framework;
using SIT.Common.Interface;
using System.IO;

namespace SIT.Common.UTest
{
    [TestFixture]
    public class CommonAppSettingsTests
    {
        private class TestDirectory(IEnvironmentHelper envHelper) : SIT.Common.Directory(envHelper)
        {

            // Override to always throw DirectoryNotFoundException
            protected static void ValidateFolderPath(string value)
            {
                throw new DirectoryNotFoundException("Test exception");
            }
        }

        [Test]
        public void InputFolder_WhenDirectoryNotFoundException_LogsErrorAndCallsExit()
        {
            // Arrange
            var envHelperMock = new Mock<IEnvironmentHelper>();
            bool exitCalled = false;
            envHelperMock.Setup(e => e.CallEnvironmentExit(It.IsAny<int>())).Callback(() => exitCalled = true);

            var testDirectory = new TestDirectory(envHelperMock.Object)
            {
                // Act
                InputFolder = "nonexistent_path"
            };

            // Assert
            Assert.IsTrue(exitCalled, "Environment exit should be called on DirectoryNotFoundException.");
        }
    }
}
