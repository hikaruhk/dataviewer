namespace DataViewer.Messages
{
    public class ChunkData
    {
        public byte[] Chunk { get; set; }

        public ChunkData(byte[] chunk)
        {
            Chunk = chunk;
        }
    }
}
