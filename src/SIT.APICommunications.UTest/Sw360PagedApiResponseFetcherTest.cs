// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using SIT.APICommunications.Model;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace SIT.APICommunications.UTest
{
    [TestFixture]
    public class Sw360PagedApiResponseFetcherTest
    {
        private static int GetPageNumberFromUrl(HttpRequestMessage request)
        {
            System.Text.RegularExpressions.Match match = Regex.Match(request.RequestUri.ToString(), "page=(\\d+)");
            return int.Parse(match.Groups[1].Value);
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/hal+json")
            };
        }

        #region FetchAllPagesStrictAsync

        [Test]
        public async Task FetchAllPagesStrictAsync_SinglePage_ReturnsFirstPageJson()
        {
            // Arrange
            string page0Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compA\"}]},\"page\":{\"totalPages\":1}}";

            // Act
            string result = await Sw360PagedApiResponseFetcher.FetchAllPagesStrictAsync<ComponentsModel, Sw360Components>(
                _ => Task.FromResult(JsonResponse(HttpStatusCode.OK, page0Json)));

            // Assert
            Assert.That(result, Does.Contain("compA"));
        }

        [Test]
        public async Task FetchAllPagesStrictAsync_MultiplePages_MergesItemsFromAllPages()
        {
            // Arrange
            string page0Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compA\"}]},\"page\":{\"totalPages\":2}}";
            string page1Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compB\"}]},\"page\":{\"totalPages\":2}}";

            // Act
            string result = await Sw360PagedApiResponseFetcher.FetchAllPagesStrictAsync<ComponentsModel, Sw360Components>(
                page => Task.FromResult(JsonResponse(HttpStatusCode.OK, page == 0 ? page0Json : page1Json)));

            // Assert
            Assert.That(result, Does.Contain("compA"));
            Assert.That(result, Does.Contain("compB"));
        }

        [Test]
        public void FetchAllPagesStrictAsync_FirstPageUnsuccessful_ThrowsHttpRequestException()
        {
            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() =>
                Sw360PagedApiResponseFetcher.FetchAllPagesStrictAsync<ComponentsModel, Sw360Components>(
                    _ => Task.FromResult(JsonResponse(HttpStatusCode.InternalServerError, string.Empty))));
        }

        [Test]
        public async Task FetchAllPagesStrictAsync_FirstPageNotJson_ReturnsRawContent()
        {
            // Arrange
            string nonJsonContent = "<html>not json</html>";

            // Act
            string result = await Sw360PagedApiResponseFetcher.FetchAllPagesStrictAsync<ComponentsModel, Sw360Components>(
                _ => Task.FromResult(JsonResponse(HttpStatusCode.OK, nonJsonContent)));

            // Assert
            Assert.That(result, Is.EqualTo(nonJsonContent));
        }

        [Test]
        public async Task FetchAllPagesStrictAsync_RemainingPageNotJson_IsSkippedDuringMerge()
        {
            // Arrange
            string page0Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compA\"}]},\"page\":{\"totalPages\":2}}";
            string page1NonJson = "<html>error</html>";

            // Act
            string result = await Sw360PagedApiResponseFetcher.FetchAllPagesStrictAsync<ComponentsModel, Sw360Components>(
                page => Task.FromResult(JsonResponse(HttpStatusCode.OK, page == 0 ? page0Json : page1NonJson)));

            // Assert
            Assert.That(result, Does.Contain("compA"));
        }

        #endregion

        #region FetchAllPagesTolerantAsync

        [Test]
        public async Task FetchAllPagesTolerantAsync_SinglePage_ReturnsMergedContent()
        {
            // Arrange
            string page0Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compA\"}]},\"page\":{\"totalPages\":1,\"totalElements\":1}}";
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(JsonResponse(HttpStatusCode.OK, page0Json));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            HttpResponseMessage response = await Sw360PagedApiResponseFetcher.FetchAllPagesTolerantAsync(httpClient, "http://test.com/components", 10);
            string content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Does.Contain("compA"));
        }

        [Test]
        public async Task FetchAllPagesTolerantAsync_MultiplePages_MergesEmbeddedArrays()
        {
            // Arrange
            string page0Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compA\"}]},\"page\":{\"totalPages\":2,\"totalElements\":2}}";
            string page1Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compB\"}]},\"page\":{\"totalPages\":2,\"totalElements\":2}}";
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
                    JsonResponse(HttpStatusCode.OK, GetPageNumberFromUrl(request) == 0 ? page0Json : page1Json));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            HttpResponseMessage response = await Sw360PagedApiResponseFetcher.FetchAllPagesTolerantAsync(httpClient, "http://test.com/components", 10);
            string content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.That(content, Does.Contain("compA"));
            Assert.That(content, Does.Contain("compB"));
        }

        [Test]
        public async Task FetchAllPagesTolerantAsync_FirstPageUnsuccessful_ReturnsEmptyResultWithOriginalStatusAndContent()
        {
            // Arrange
            string errorContent = "Not Found";
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(JsonResponse(HttpStatusCode.NotFound, errorContent));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            HttpResponseMessage response = await Sw360PagedApiResponseFetcher.FetchAllPagesTolerantAsync(httpClient, "http://test.com/components", 10);
            string content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(content, Is.EqualTo(errorContent));
        }

        [Test]
        public async Task FetchAllPagesTolerantAsync_FirstPageSuccessButNotJson_ReturnsOriginalContentAsEmptyResult()
        {
            // Arrange
            string nonJsonContent = "no match";
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(JsonResponse(HttpStatusCode.OK, nonJsonContent));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            HttpResponseMessage response = await Sw360PagedApiResponseFetcher.FetchAllPagesTolerantAsync(httpClient, "http://test.com/components", 10);
            string content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Is.EqualTo(nonJsonContent));
        }

        [Test]
        public void FetchAllPagesTolerantAsync_RemainingPageUnsuccessful_ThrowsHttpRequestException()
        {
            // Arrange
            string page0Json = "{\"_embedded\":{\"sw360:components\":[{\"name\":\"compA\"}]},\"page\":{\"totalPages\":2,\"totalElements\":2}}";
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
                    GetPageNumberFromUrl(request) == 0
                        ? JsonResponse(HttpStatusCode.OK, page0Json)
                        : JsonResponse(HttpStatusCode.InternalServerError, string.Empty));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(() =>
                Sw360PagedApiResponseFetcher.FetchAllPagesTolerantAsync(httpClient, "http://test.com/components", 10));
        }

        #endregion

        #region GetPageAsync

        [Test]
        public async Task GetPageAsync_BaseUrlWithoutQuery_BuildsUrlWithQuestionMarkSeparator()
        {
            // Arrange
            HttpRequestMessage capturedRequest = null;
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            await Sw360PagedApiResponseFetcher.GetPageAsync(httpClient, "http://test.com/components", 2, 50);

            // Assert
            Assert.That(capturedRequest.RequestUri.ToString(), Is.EqualTo("http://test.com/components?page=2&page_entries=50"));
        }

        [Test]
        public async Task GetPageAsync_BaseUrlWithExistingQuery_BuildsUrlWithAmpersandSeparator()
        {
            // Arrange
            HttpRequestMessage capturedRequest = null;
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            await Sw360PagedApiResponseFetcher.GetPageAsync(httpClient, "http://test.com/components?name=foo", 0, 10);

            // Assert
            Assert.That(capturedRequest.RequestUri.ToString(), Is.EqualTo("http://test.com/components?name=foo&page=0&page_entries=10"));
        }

        [Test]
        public async Task GetPageAsync_WithExtraQueryParams_NormalizesLeadingSeparators()
        {
            // Arrange
            HttpRequestMessage capturedRequest = null;
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handlerMock.Object);

            // Act
            await Sw360PagedApiResponseFetcher.GetPageAsync(httpClient, "http://test.com/components", 1, 25, "&allDetails=true");

            // Assert
            Assert.That(capturedRequest.RequestUri.ToString(), Is.EqualTo("http://test.com/components?allDetails=true&page=1&page_entries=25"));
        }

        #endregion
    }
}
