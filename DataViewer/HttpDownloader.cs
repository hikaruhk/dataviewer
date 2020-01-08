using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.IO;
using DataViewer.Messages;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataViewer
{
    public class StreamReceiver : ReceiveActor
    {
        private readonly IActorRef _actorRef;
        private readonly IActorRef _streamReceiver;
        private readonly MemoryStream _stream;

        private const int BufferSize = 4096;

        public StreamReceiver(IActorRef actorRef)
        {
            _stream = new MemoryStream();
            _streamReceiver = Source
                .ActorRef<ChunkData>(BufferSize, OverflowStrategy.Fail)
                .Via(Flow.Create<ChunkData>().Select(s => ByteString.CopyFrom(s.Chunk)))
                .To(StreamConverters.FromOutputStream(() => _stream, true))
                .Run(Context.Materializer());

            _actorRef = actorRef;

            Receive<ChunkData>(message => ReceivedStreamChunk(message));
            Receive<DownloadComplete>(message => ReceivedStreamComplete(message));
        }

        private void ReceivedStreamChunk(ChunkData chunkData)
        {
            Console.WriteLine(
                $"[{nameof(StreamReceiver)}] receive chunk sized {chunkData.Chunk.Length}, previous stream length {_stream.Length}");
            _streamReceiver.Forward(chunkData);
        }

        private void ReceivedStreamComplete(DownloadComplete message)
        {
            Console.WriteLine($"[{nameof(StreamReceiver)}] got signaled that the stream completed.");
            _actorRef.Tell(new HttpResult<Stream>() { FailedMessage = $"[{nameof(StreamReceiver)}] Stream total length was, {_stream.Length}" });

            _stream.Close();
            Self.Tell(PoisonPill.Instance);
        }
    }

    public class ChunkData
    {
        public byte[] Chunk { get; set; }

        public ChunkData(byte[] chunk)
        {
            Chunk = chunk;
        }
    }

    public class DownloadComplete
    {

    }

    public class HttpDownloader : ReceiveActor
    {
        private readonly IHttpClientFactory _clientFactory;
        private IActorRef _originalSender;

        public HttpDownloader(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            Become(Begin);
        }

        public static Props GetProp(IHttpClientFactory clientFactory) =>
            Props.Create(() => new HttpDownloader(clientFactory));

        private void Begin()
        {
            Receive<HttpRequest>(request =>
            {
                var self = Self;
                var context = Context;
                _originalSender = Sender;
                _clientFactory.CreateClient(request.Uri.ToString())
                    .GetAsync(request.Uri)
                    .ContinueWith(httpRequest =>
                    {
                        var materializer = context.Materializer();
                        var streamResult = httpRequest.Result.Content.ReadAsStreamAsync().Result;
                        var streamReceiver = context.ActorOf(Props.Create(() => new StreamReceiver(self)));

                        var result = StreamConverters
                            .FromInputStream(() => streamResult)
                            .Via(Flow.Create<ByteString>().Select(s => new ChunkData(s.ToArray())))
                            .To(Sink.ActorRef<ChunkData>(streamReceiver, new DownloadComplete()))
                            .Run(materializer);
                    })
                    .PipeTo(self);
            }, request => request.Action == HttpMethod.Get);

            Receive<HttpResult<Stream>>(result =>
            {
                var message = $"{result.StatusCode}, {result.Uri} -> ${result.FailedMessage}";
                _originalSender.Tell(message);
            });
        }
    }
}
