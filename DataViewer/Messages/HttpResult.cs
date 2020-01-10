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
        public T Response { get; set; }
    }
}
