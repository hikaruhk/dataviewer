using System;
using System.Net;

namespace DataViewerTests
{
    internal partial class TestingBuddies
    {
        public class MockedHttpClientParameters
        {
            public Uri RequestUri { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public string Content { get; set; }
            public int Delay { get; set; }
        }
    }
}
