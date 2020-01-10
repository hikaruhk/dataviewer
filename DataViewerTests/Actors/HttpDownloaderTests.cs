using Akka.Actor;
using Akka.Streams;
using Akka.Streams.IO;
using Akka.TestKit.NUnit;
using AutoBogus;
using DataViewer;
using DataViewer.Actors;
using DataViewer.Messages;
using DataViewer.Messages.Enums;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static DataViewerTests.TestingBuddies;

namespace DataViewerTests.Actors
{
    [TestFixture]
    public class HttpDownloaderTests : TestKit
    {
        [TestCase(HttpStatusCode.OK, "https://www.google.com/", "Content from A", TestName = "Downloader returning content stream")]
        [TestCase(HttpStatusCode.OK, "https://www.google.com/", "", TestName = "Downloader returning nothing from stream")]
        public async Task ShouldDownloadHttpData(
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

        [Test]
        public void ShouldDownloadHttpDataUsingChildren()
        {
            var httpClient = CreateMockedHttpClientFactory(new MockedHttpClientParameters
            {
                StatusCode = HttpStatusCode.OK,
                RequestUri = new Uri("https://google.com"),
                Content = "SomeContent"
            });

            var materializer = Sys.Materializer(namePrefix: "testMaterializer");
            var actor = Sys.ActorOf(
                Props.Create(() => new HttpDownloader(materializer, httpClient)),
                "TestHttpDownloader");

            actor.Tell(new HttpRequest { Uri = new Uri("https://google.com"), Action = HttpMethod.Get });

            ExpectMsgAllOf<HttpResult<string>>();
        }
    }
}
