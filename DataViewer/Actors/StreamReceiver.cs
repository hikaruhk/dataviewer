using Akka.Actor;
using Akka.Event;
using DataViewer.Messages;

namespace DataViewer.Actors
{
    public class StreamReceiver : ReceiveActor
    {
        private const int _bufferSize = 4096;
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private int _total = 0;

        public StreamReceiver()
        {
            Receive<DownloadStart>(message => ReceiveSignal(message));
            Receive<ChunkData>(message => ReceivedStreamChunk(message));
            Receive<DownloadComplete>(message => ReceivedStreamComplete(message));
        }

        public void ReceiveSignal(DownloadStart message) =>
            _logger.Info($"[{nameof(StreamReceiver)}] Init download");

        private void ReceivedStreamChunk(ChunkData chunkData)
        {
            _total += chunkData.Chunk.Length;
        }

        private void ReceivedStreamComplete(DownloadComplete message)
        {
            _logger.Info($"[{nameof(StreamReceiver)}] got signaled that the stream completed.");
            Context.Parent.Tell(
                new HttpResult<string>
                {
                    Response = $"Stream total length was, {_total}" 
                });

            message.Stream.Flush();
            message.Stream.Dispose();
        }
    }
}
