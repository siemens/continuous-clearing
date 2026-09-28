using SIT.APICommunications.Model;
using System.Net;
using System.Text;

namespace SIT.APICommunications.UTest
{
    [TestFixture]
    public class JfrogAqlApiCommunicationUTest
    {

        [Test]
        public void JfrogAqlApiCommunication_CheckConnection_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.CheckConnection());
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
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetInternalComponentDataByRepo(invalidRepoName));
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
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetNpmComponentDataByRepo(invalidRepoName));
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
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetPypiComponentDataByRepo(invalidRepoName));
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
            string expectedQuery = "items.find({\"$and\": [{ \"repo\":{ \"$eq\": \"nuget-repo\" } },{ \"$or\":[{ \"@nuget.id\":{ \"$eq\": \"TestPackage\" } },{ \"@nuget.id\":{ \"$eq\": \"testpackage\" } }]},{ \"@nuget.version\":{\"$eq\": \"1.0.0\" } }]}).include(\"repo\", \"path\", \"name\").limit(1)";
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

        [Test]
        public void JfrogAqlApiCommunication_GetCargoComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-npm-repo"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetCargoComponentDataByRepo(invalidRepoName));
        }

        [Test]
        public async Task JfrogAqlApiCommunication_GetInternalComponentDataByRepo_MergesAllPagesWhenFirstPageIsFull()
        {
            // Arrange: pagination continues only when a page returns exactly AqlPageSize items ("full"); a full
            // first page followed by a short second page must be merged into one result set.
            int port = GetFreeTcpPort();
            string prefix = $"http://127.0.0.1:{port}/";

            using HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            string fullPageJson = BuildResultsJson(ApiConstant.AqlPageSize, "full-page-item");
            string shortPageJson = BuildResultsJson(1, "n2");

            Task serverTask = Task.Run(async () =>
            {
                for (int i = 0; i < 2; i++)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    using StreamReader reader = new StreamReader(context.Request.InputStream);
                    string requestBody = await reader.ReadToEndAsync();
                    bool isFirstPage = requestBody.Contains(".offset(0)");
                    string json = isFirstPage ? fullPageJson : shortPageJson;
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer);
                    context.Response.OutputStream.Close();
                }
            });

            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials { Token = "test-token" };
            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(prefix.TrimEnd('/'), repoCredentials, 30);

            // Act
            HttpResponseMessage response = await jfrogApiCommunication.GetInternalComponentDataByRepo("test-repo");
            await serverTask;
            listener.Stop();

            // Assert
            string content = await response.Content.ReadAsStringAsync();
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(content, Does.Contain("full-page-item"));
            Assert.That(content, Does.Contain("n2"));
        }

        [Test]
        public async Task JfrogAqlApiCommunication_GetInternalComponentDataByRepo_StopsAfterShortFirstPage()
        {
            // Arrange: a first page with fewer than AqlPageSize items means no more pages exist, so only one request
            // should ever be made.
            int port = GetFreeTcpPort();
            string prefix = $"http://127.0.0.1:{port}/";

            using HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            string shortPageJson = BuildResultsJson(1, "only-item");
            int requestCount = 0;

            Task serverTask = Task.Run(async () =>
            {
                HttpListenerContext context = await listener.GetContextAsync();
                Interlocked.Increment(ref requestCount);
                byte[] buffer = Encoding.UTF8.GetBytes(shortPageJson);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer);
                context.Response.OutputStream.Close();
            });

            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials { Token = "test-token" };
            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(prefix.TrimEnd('/'), repoCredentials, 30);

            // Act
            HttpResponseMessage response = await jfrogApiCommunication.GetInternalComponentDataByRepo("test-repo");
            await serverTask;
            listener.Stop();

            // Assert
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(requestCount, Is.EqualTo(1));
        }

        private static string BuildResultsJson(int itemCount, string namePrefix)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{\"results\":[");
            for (int i = 0; i < itemCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }
                builder.Append($"{{\"repo\":\"test-repo\",\"path\":\"p{i}\",\"name\":\"{namePrefix}\"}}");
            }
            builder.Append("]}");
            return builder.ToString();
        }

        private static int GetFreeTcpPort()
        {
            System.Net.Sockets.TcpListener tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }
    }
}
