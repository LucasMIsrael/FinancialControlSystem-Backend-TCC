using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class AiConnectionTests
    {
        private DashboardsAppService CreateService(HttpResponseMessage fakeResponse)
        {
            // Simula HttpClient
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler.Protected()
                       .Setup<Task<HttpResponseMessage>>(
                             "SendAsync",
                              ItExpr.IsAny<HttpRequestMessage>(),
                              ItExpr.IsAny<CancellationToken>()
                       )
                       .ReturnsAsync(fakeResponse);

            var httpClient = new HttpClient(mockHandler.Object);

            // Mock da sessão
            var mockSession = new Mock<IAppSession>();
            mockSession.Setup(x => x.UserId).Returns(123);
            mockSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            // Mock dos repositórios
            var envRepo = new Mock<IGeneralRepository<Environments>>();
            var goalsRepo = new Mock<IGeneralRepository<Goals>>();
            var plannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var unplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            // Mock das configurações contendo a KEY
            var apiSettings = new ApiSettings { Key = "TEST_API_KEY" };
            var mockOptions = new Mock<IOptions<ApiSettings>>();
            mockOptions.Setup(o => o.Value).Returns(apiSettings);

            return new DashboardsAppService(
                mockSession.Object,
                envRepo.Object,
                goalsRepo.Object,
                plannedRepo.Object,
                unplannedRepo.Object,
                mockOptions.Object,
                httpClient
            );
        }

        [Fact]
        public async Task ShouldReturnTextWhenApiReturnsSuccess()
        {
            // Arrange — resposta simulada da API Gemini
            string fakeJson = @"
            {
                ""candidates"": [
                    {
                        ""content"": {
                            ""parts"": [
                                { ""text"": ""Resposta da IA aqui"" }
                            ]
                        }
                    }
                ]
            }";

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fakeJson)
            };

            var service = CreateService(fakeResponse);

            // Act
            string result = await service.GeminiConnection("minha pergunta");

            // Assert
            Assert.Equal("Resposta da IA aqui", result);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenApiReturnsError()
        {
            // Arrange
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid request\"}")
            };

            var service = CreateService(fakeResponse);

            // Act
            var exception = await Record.ExceptionAsync(() => service.GeminiConnection("pergunta"));

            // Assert
            Assert.NotNull(exception);
            Assert.Contains("Erro da API Gemini", exception.Message);
        }

        [Fact]
        public async Task ShouldSendCorrectHttpRequest()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock.Protected()
                       .Setup<Task<HttpResponseMessage>>(
                           "SendAsync",
                           ItExpr.Is<HttpRequestMessage>(req =>
                               req.Method == HttpMethod.Post &&
                               req.RequestUri.ToString()
                                             .Contains("gemini-2.5-flash-preview-09-2025:generateContent?key=TEST_API_KEY")
                           ),
                           ItExpr.IsAny<CancellationToken>()
                       )
                       .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                       {
                           Content = new StringContent(@"
                           {
                               ""candidates"": [
                                   {
                                       ""content"": {
                                           ""parts"": [
                                               { ""text"": ""OK"" }
                                           ]
                                       }
                                   }
                               ]
                           }")
                       })
                       .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);

            var mockSession = new Mock<IAppSession>();
            mockSession.Setup(x => x.UserId).Returns(123);
            mockSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            var mockOptions = new Mock<IOptions<ApiSettings>>();
            mockOptions.Setup(o => o.Value).Returns(new ApiSettings { Key = "TEST_API_KEY" });

            var service = new DashboardsAppService(
                mockSession.Object,
                new Mock<IGeneralRepository<Environments>>().Object,
                new Mock<IGeneralRepository<Goals>>().Object,
                new Mock<IGeneralRepository<PlannedExpensesAndProfits>>().Object,
                new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>().Object,
                mockOptions.Object,
                httpClient
            );

            // Act
            var result = await service.GeminiConnection("teste");

            // Assert
            Assert.Equal("OK", result);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}