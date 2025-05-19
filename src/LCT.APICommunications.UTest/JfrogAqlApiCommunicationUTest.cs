using LCT.APICommunications.Model;

namespace LCT.APICommunications.UTest
{
    [TestFixture]
    public class JfrogAqlApiCommunicationUTest
    {


        [Test]
        public void JfrogAqlApiCommunication_CheckConnection_ReturnsInvalidOperationException()
        {
            // Arrange
            string correlationId = Guid.NewGuid().ToString();
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.CheckConnection(correlationId));
        }

        [Test]
        public void JfrogAqlApiCommunication_GetInternalComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-repo-name"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetInternalComponentDataByRepo(invalidRepoName,""));
        }
        [Test]
        public void JfrogAqlApiCommunication_GetNpmComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-npm-repo"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetNpmComponentDataByRepo(invalidRepoName, ""));
        }
        [Test]
        public void JfrogAqlApiCommunication_GetPypiComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-pypi-repo"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetPypiComponentDataByRepo(invalidRepoName, ""));
        }
        [Test]
        public void JfrogAqlApiCommunication_GetPackageInfo_ReturnsArgumentException_WhenNoPackageNameOrPathProvided()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Create a ComponentsToArtifactory object with invalid parameters (both packageName and path are null or empty)
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                JfrogPackageName = null,
                Path = null
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await jfrogApiCommunication.GetPackageInfo(component));
        }
        [Test]
        public void BuildAqlQuery_NPMComponent_ReturnsValidQuery()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "NPM",
                SrcRepoName = "npm-repo",
                Name = "test-package",
                Version = "1.0.0"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"repo\":{\"$eq\":\"npm-repo\"}, \"@npm.name\":{\"$eq\":\"test-package\"}, \"@npm.version\":{\"$eq\":\"1.0.0\"}}).include(\"repo\", \"path\", \"name\")";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }

        [Test]
        public void BuildAqlQuery_PythonComponent_ReturnsValidQuery()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "Python",
                SrcRepoName = "pypi-repo",
                Name = "test-package",
                Version = "1.0.0"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"repo\":{\"$eq\":\"pypi-repo\"}, \"@pypi.normalized.name\":{\"$eq\":\"test-package\"}, \"@pypi.version\":{\"$eq\":\"1.0.0\"}}).include(\"repo\", \"path\", \"name\")";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }

        [Test]
        public void BuildAqlQuery_NugetComponent_ReturnsValidQuery()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "Nuget",
                SrcRepoName = "nuget-repo",
                Name = "TestPackage",
                Version = "1.0.0"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"$and\": [{ \"repo\":{ \"$eq\": \"nuget-repo\" } },{ \"$or\":[{ \"@nuget.id\":{ \"$eq\": \"TestPackage\" } } ,{ \"@nuget.id\":{ \"$eq\": \"testpackage\" } }] },{ \"@nuget.version\":{\"$eq\": \"1.0.0\" } }]}).include(\"repo\", \"path\", \"name\").limit(1)";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }

        [Test]
        public void BuildAqlQuery_OtherComponentWithPathAndName_ReturnsValidQuery()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "Other",
                SrcRepoName = "generic-repo",
                Path = "test/path",
                JfrogPackageName = "test-package"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"repo\":{\"$eq\":\"generic-repo\"}, \"path\":{\"$match\":\"test/path\"}, \"name\":{\"$match\":\"test-package\"}}).include(\"repo\", \"path\", \"name\").limit(1)";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }

        [Test]
        public void BuildAqlQuery_OtherComponentWithoutPathAndName_ReturnsBasicQuery()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "Other",
                SrcRepoName = "generic-repo"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"repo\":{\"$eq\":\"generic-repo\"}}).include(\"repo\", \"path\", \"name\").limit(1)";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }

        [TestCase("npm")]
        [TestCase("NPM")]
        [TestCase("Npm")]
        public void BuildAqlQuery_NPMComponentCaseInsensitive_ReturnsValidQuery(string componentType)
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = componentType,
                SrcRepoName = "npm-repo",
                Name = "test-package",
                Version = "1.0.0"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"repo\":{\"$eq\":\"npm-repo\"}, \"@npm.name\":{\"$eq\":\"test-package\"}, \"@npm.version\":{\"$eq\":\"1.0.0\"}}).include(\"repo\", \"path\", \"name\")";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }


        [Test]
        public void BuildAqlQuery_EmptyComponentType_ReturnsBasicQuery()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "",
                SrcRepoName = "generic-repo"
            };

            // Act
            string query = JfrogAqlApiCommunication.BuildAqlQuery(component);

            // Assert
            string expectedQuery = "items.find({\"repo\":{\"$eq\":\"generic-repo\"}}).include(\"repo\", \"path\", \"name\").limit(1)";
            Assert.That(query, Is.EqualTo(expectedQuery));
        }


    }
}
