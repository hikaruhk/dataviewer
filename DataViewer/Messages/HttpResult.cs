using DataViewer.Messages.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DataViewer.Messages
{
    public class HttpResult<T>
    {
        public string Uri { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T Content { get; set; }
        public HttpMethod Action { get; set; }
        public string FailedMessage { get; set; }
    }
}
