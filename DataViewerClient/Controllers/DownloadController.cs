using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using DataViewer;
using DataViewer.Messages;
using DataViewer.Messages.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataViewerClient.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly IActorRef _actorRef;
        public DownloadController(
            IActorRef actorRef)
        {
            _actorRef = actorRef;
        }

        [HttpGet("DownloadFromUrl")]
        public async Task<IActionResult> Get(string uri)
        {
            var result = await _actorRef.Ask(new HttpRequest{ Uri = new Uri(uri), Action = HttpMethod.Get });

            return Ok($"Length is = ${result}");
        }
    }
}
