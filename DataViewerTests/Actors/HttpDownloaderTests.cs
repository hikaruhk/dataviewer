using Akka.Actor;
using Akka.TestKit.NUnit;
using AutoBogus;
using DataViewer;
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

            var actor = Sys.ActorOf(Props.Create(() => new HttpDownloader(httpClient)));

            var result = await actor
                .Ask<Stream>(new HttpRequest
                { 
                    Uri = new Uri(url),
                    Action = HttpMethod.Get
                });

            using var streamReaderResult = new StreamReader(result);
            var stringResult = await streamReaderResult.ReadToEndAsync();

            Assert.AreEqual(content, stringResult);
        }

        [TestCase(HttpStatusCode.OK, "https://www.abc.com/", 500, Description = "Downloaded content timesout")]
        public void ShouldTimeoutLongDownloads(
            HttpStatusCode status,
            string url,
            int timeout)
        {
            var httpClient = CreateMockedHttpClientFactory(new MockedHttpClientParameters
            {
                StatusCode = status,
                RequestUri = new Uri(url),
                Content = "Content from A",
                Delay = timeout * 2
            });

            var actor = Sys.ActorOf(Props.Create(() => new HttpDownloader(httpClient)));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Content from A"));

            Assert.ThrowsAsync<Exception>(async () =>
            {
                var reuslt = await actor.Ask<Stream>(new HttpRequest
                {
                    Uri = new Uri(url),
                    Action = HttpMethod.Get,
                    Timeout = timeout
                });
            });
        }

        [Test]
        public void Test()
        {
            var uri = new Uri("https://raw.githubusercontent.com/prust/wikipedia-movie-data/master/movies.json");
            var httpClient = CreateMockedHttpClientFactory(new MockedHttpClientParameters
            {
                StatusCode = HttpStatusCode.OK,
                RequestUri = uri,
                Content = "Content from A",
                Delay = 1
            });

            using var system = ActorSystem.Create("dataviewer");
            var props = Props.Create(() => new HttpDownloader(httpClient));

            var actor = system.ActorOf(props, "test1");

            actor.Tell(new HttpRequest { Uri = uri, Action = HttpMethod.Get });
            actor.Tell(new HttpRequest { Uri = uri, Action = HttpMethod.Get });
            actor.Tell(new HttpRequest { Uri = uri, Action = HttpMethod.Get });
            actor.Tell(new HttpRequest { Uri = uri, Action = HttpMethod.Get });
            actor.Tell(new HttpRequest { Uri = uri, Action = HttpMethod.Get });
        }
    }
}
