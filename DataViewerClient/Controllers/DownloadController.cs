using Akka.Actor;
using DataViewer.Messages;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

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
            var results = await _actorRef.Ask<string>(
                new HttpRequest
                { 
                    Uri = new Uri(uri), 
                    Action = HttpMethod.Get 
                });

            return Ok(results);
        }
    }
}
