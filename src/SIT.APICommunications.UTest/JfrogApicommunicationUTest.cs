// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications.Interfaces;
using SIT.APICommunications.Model;

namespace SIT.APICommunications.UTest
{
    [TestFixture]
    public class JfrogApicommunicationUTest
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public void JfrogApicommunication_CopyPackageFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            IJFrogApiCommunication jfrogApicommunication = new NugetJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.DeletePackageFromJFrogRepo("", ""));
        }
    }
}
