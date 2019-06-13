using System;
using System.Collections.Generic;
using Roentgenium.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Roentgenium.Controllers
{
    /// <summary>Query job status</summary>
    [Route("[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly IPipelineManager _pipelineManager;

        /// <summary></summary>
        public StatusController(IPipelineManager pipelineManager)
        {
            _pipelineManager = pipelineManager;
        }

        /// <summary>Query job status</summary>
        /// <remarks>
        /// If <a href="/info/limits/completedjobexpirytime" target="_blank"> 
        /// <code>CompletedJobExpiryTime</code></a> is set to a valid value, this will only return data for completed 
        /// jobs that haven't yet been expired. Simple <code>GET</code>-generated jobs are never tracked after completion
        /// and therefore will never be included in the job status report.
        /// </remarks>
        /// <param name="jobId">The ID of the job for which to retrieve status</param>
        [HttpGet("{jobId}")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [Produces("application/json")]
        public JsonResult Get(Guid jobId)
        {
            var stat = _pipelineManager.GetStatus(jobId);
            var retVal = new Dictionary<string, object>()
            {
                {"status", PipelineBase.PipelineStatusNames[stat] }
            };

            switch (stat)
            {
                case PipelineBase.PipelineStatus.Executing:
                case PipelineBase.PipelineStatus.Persisting:
                    retVal["elapsed"] = _pipelineManager.GetElapsed(jobId);
                    if (stat == PipelineBase.PipelineStatus.Executing)
                        retVal["progress"] = _pipelineManager.GetProgress(jobId);
                    else
                        retVal["progress"] = _pipelineManager.GetPersistenceStatus(jobId);
                    break;
                case PipelineBase.PipelineStatus.Success:
                    retVal["results"] = _pipelineManager.GetResults(jobId);
                    break;
                default: break;
            }

            return new JsonResult(retVal);
        }
    }
}