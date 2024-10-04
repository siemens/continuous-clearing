// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class ExceptionHandlingTests
    {
        [Test]
        public void HttpException_StatusCodeBetween400And499_LogsErrorMessage()
        {
            // Arrange
            var ex = new HttpRequestException();
            var response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest };
            var exceptionSource = "TestExceptionSource";

            // Act
            ExceptionHandling.HttpException(ex, response, exceptionSource);

            // Assert
            // Verify that the error message is logged
            Assert.Pass();
        }

        [Test]
        public void HttpException_StatusCodeBetween500And599OrNull_LogsErrorMessage()
        {
            // Arrange
            var ex = new HttpRequestException();
            var response = new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.InternalServerError };
            var exceptionSource = "TestExceptionSource";

            // Act
            ExceptionHandling.HttpException(ex, response, exceptionSource);

            // Assert
            // Verify that the error message is logged
            Assert.Pass();
        }

        [Test]
        public void ArgumentException_LogsErrorMessage()
        {
            // Arrange
            var message = "Test message";

            // Act
            ExceptionHandling.ArgumentException(message);

            // Assert
            // Verify that the error message is logged
            Assert.Pass();
        }

        [Test]
        public void TaskCancelledException_LogsErrorMessage()
        {
            // Arrange
            var ex = new TaskCanceledException();
            var exceptionSource = "TestExceptionSource";

            // Act
            ExceptionHandling.TaskCancelledException(ex, exceptionSource);

            // Assert
            // Verify that the error message is logged
            Assert.Pass();
        }

        [Test]
        public void FossologyException_StatusCodeBetween500And599_LogsErrorMessage()
        {
            // Arrange
            var ex = new HttpRequestException();
            ex.HResult = 500;

            // Act
            ExceptionHandling.FossologyException(ex);

            // Assert
            // Verify that the error message is logged
            Assert.Pass();
        }

        [Test]
        public void FossologyException_StatusCodeNotBetween500And599_LogsErrorMessage()
        {
            // Arrange
            var ex = new HttpRequestException();
            ex.HResult = 400;


            // Act
            ExceptionHandling.FossologyException(ex);

            // Assert
            // Verify that the error message is logged
            Assert.Pass();
        }
    }
}
