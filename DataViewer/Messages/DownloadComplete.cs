using System.IO;

namespace DataViewer.Messages
{
    public class DownloadComplete
    {
        public Stream Stream { get; }
        public DownloadComplete(Stream stream)
        {
            Stream = stream;
        }
    }
}
