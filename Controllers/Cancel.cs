using System;
using Roentgenium.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Roentgenium.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CancelController : ControllerBase
    {
        private readonly IPipelineManager _pipelineManager;

        public CancelController(IPipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager;
        }

        /// <summary>Cancel a running job</summary>
        /// <remarks>
        /// Job cancelation is only guaranteed if the job in question still has status
        /// <code>Executing</code>: once a job has moved into the <code>Persisting</code> state, cancelation
        /// will be <em>attempted</em> but cannot be guaranteed, as no currently-in-process persistence
        /// operation will be interrupted (e.g. no incomplete artifacts will be created).
        /// </remarks>
        /// <param name="jobId">The ID of the job to be cancelled</param>
        /// <param name="queueToken">The job's queue token</param>
        [HttpGet("{jobId}/{queueToken}")]
        [Produces("application/json")]
        public JsonResult Cancel(Guid jobId, string queueToken)
        {
            return new JsonResult(_pipelineManager.Cancel(jobId, queueToken, 
                new PipelineRequestTracker() { 
                    RemoteAddr = HttpContext.Connection.RemoteIpAddress.ToString() 
                }));
        }
    }
}
