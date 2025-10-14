// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using Moq;
using NUnit.Framework;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class CommonAppSettingsTests
    {
        private class TestDirectory(IEnvironmentHelper envHelper) : LCT.Common.Directory(envHelper)
        {

            // Override to always throw DirectoryNotFoundException
            protected new void ValidateFolderPath(string value)
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

            var testDirectory = new TestDirectory(envHelperMock.Object);

            // Act
            testDirectory.InputFolder = "nonexistent_path";

            // Assert
            Assert.IsTrue(exitCalled, "Environment exit should be called on DirectoryNotFoundException.");
        }
    }
}
