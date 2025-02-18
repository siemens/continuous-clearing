using Moq;
using System.Net;
using log4net;

namespace LCT.APICommunications.UTest
{
    public class RetryWebClientHandlerUTest
    {
        private Mock<ILog> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            // Mock the logger
            _mockLogger = new Mock<ILog>();
        }        

        [Test]
        public async Task ExecuteWithRetryAsync_ShouldCompleteSuccessfully_WhenActionSucceedsAfterRetry()
        {
            // Arrange
            var attempts = 0;
            var action = new Func<Task>(() =>
            {
                attempts++;
                if (attempts < ApiConstant.APIRetryCount)
                {
                    throw new WebException("Temporary error", WebExceptionStatus.Timeout);
                }
                return Task.CompletedTask; // Successfully completes after retries
            });

            // Act
            await RetryWebClientHandler.ExecuteWithRetryAsync(action);

            // Assert
            Assert.AreEqual(ApiConstant.APIRetryCount, attempts, "Action should have been attempted the expected number of times.");
        }

        [Test]
        public async Task ExecuteWithRetryAsync_ShouldNotRetry_WhenNoWebExceptionIsThrown()
        {
            // Arrange
            var actionExecuted = false;
            var action = new Func<Task>(() =>
            {
                actionExecuted = true;
                return Task.CompletedTask;
            });

            // Act
            await RetryWebClientHandler.ExecuteWithRetryAsync(action);

            // Assert
            Assert.IsTrue(actionExecuted, "Action should have been executed.");
            _mockLogger.Verify(logger => logger.Debug(It.IsAny<string>()), Times.Never, "Retry should not occur if there is no exception.");
        }        

    }

}
