using DataViewer.Messages.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DataViewer.Messages
{
    public class HttpRequest
    {
        public Uri Uri { get; set; }
        public HttpMethod Action { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
        public int Retries { get; set; } = 1;
        public int Timeout { get; set; } = 10000;
    }
}
