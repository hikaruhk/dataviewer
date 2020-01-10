using Akka.Actor;
using Akka.Streams;
using Akka.TestKit.NUnit;
using DataViewer.Actors;
using DataViewer.Messages;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static DataViewerTests.TestingBuddies;

namespace DataViewerTests.Actors
{
    [TestFixture]
    public class HttpDownloaderTests : TestKit
    {
        [TestCase(HttpStatusCode.OK, "https://www.abc.com/", "Content from A", Description = "Downloader returning content stream")]
        public async Task ShouldDownloadData(
            HttpStatusCode status,
            string url,
            string content)
        {
            var httpClient = CreateMockedHttpClientFactory(new MockedHttpClientParameters
            {
                StatusCode = status,
                RequestUri = new Uri(url),
                Content = content
            });

            var materializer = Sys.Materializer();
            var actor = Sys.ActorOf(Props.Create(() => new HttpDownloader(materializer, httpClient)));

            var result = await actor
                .Ask<string>(new HttpRequest
                {
                    Uri = new Uri(url),
                    Action = HttpMethod.Get
                });

            Assert.AreEqual(
                $"[{nameof(HttpDownloader)}] Stream total length was, {content.Length}",
                result,
                "Output message stream length differs in bytes.");
        }
    }
}
