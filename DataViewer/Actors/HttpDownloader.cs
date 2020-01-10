using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.IO;
using Akka.Streams.Util;
using DataViewer.Messages;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataViewer.Actors
{
    public class HttpDownloader : ReceiveActor
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IMaterializer _materializer;
        private IActorRef _originalSender;

        public HttpDownloader(IMaterializer materializer, IHttpClientFactory clientFactory)
        {
            _materializer = materializer;
            _clientFactory = clientFactory;

            Become(Begin);
        }

        public static Props GetProp(IMaterializer materializer, IHttpClientFactory clientFactory) =>
            Props.Create(() => new HttpDownloader(materializer, clientFactory));

        private void Begin()
        {
            var context = Context;
            Receive<HttpRequest>(request =>
            {
                _originalSender = Sender;
                _clientFactory
                    .CreateClient(request.Uri.ToString())
                    .GetAsync(request.Uri)
                    .ContinueWith(httpRequest =>
                    {
                        httpRequest
                            .Result
                            .Content
                            .ReadAsStreamAsync()
                            .PipeTo(context.Self);
                    });
            }, request => request.Action == HttpMethod.Get);
            Receive<Stream>(stream =>
            {
                var streamReceiver = context.ActorOf(Props.Create(() => new StreamReceiver()));

                StreamConverters
                    .FromInputStream(() => stream)
                    .Named($"inputStream-{Guid.NewGuid().ToString()}")
                    .Recover(r => 
                        ByteString
                            .FromString($"inputStream, Error message! - {r.Message}"))
                    .Via(Flow
                        .Create<ByteString>()
                        .Select(s => new ChunkData(s.ToArray())))
                    .To(Sink
                        .ActorRef<ChunkData>(
                            streamReceiver,
                            new DownloadComplete(stream)))
                    .Run(_materializer)
                    .PipeTo(context.Self);
            });
            Receive<IOResult>(result => 
                Console.WriteLine($"[{nameof(HttpDownloader)}] {result.Count} bytes processed"));
            Receive<HttpResult<string>>(result =>
            {
                _originalSender.Tell($"[{nameof(HttpDownloader)}] {result.Response}");
                Context.Stop(Sender);
            });
        }
    }
}
