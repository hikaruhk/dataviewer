using Moq;
using Moq.Protected;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataViewerTests
{
    internal partial class TestingBuddies
    {
        public static HttpClient CreateMockedHttpClient(MockedHttpClientParameters parameters)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(i => i.RequestUri == parameters.RequestUri),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    Thread.Sleep(parameters.Delay);
                    return new HttpResponseMessage
                    {
                        StatusCode = parameters.StatusCode,
                        Content = new StringContent(parameters.Content)
                    };
                });

            return new HttpClient(handler.Object);
        }

        public static IHttpClientFactory CreateMockedHttpClientFactory(
            params MockedHttpClientParameters[] clientParameters)
        {
            var mockedHttpClientFactory = new Mock<IHttpClientFactory>();

            foreach (var parameter in clientParameters)
            {
                mockedHttpClientFactory
                            .Setup(s => s.CreateClient(
                                It.Is<string>(
                                    i => i == parameter.RequestUri.ToString())))
                            .Returns(CreateMockedHttpClient(parameter));
            }

            return mockedHttpClientFactory.Object;
        }
    }
}
