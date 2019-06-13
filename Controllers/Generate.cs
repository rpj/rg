using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Roentgenium.Config;
using Roentgenium.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Roentgenium.Controllers
{
    /// <summary>Creates generation jobs (the primary user input interface)</summary>
    [Route("[controller]")]
    [ApiController]
    public class GenerateController : ControllerBase
    {
        private readonly IPipelineManager _pipelineManager;
        private readonly Dictionary<Type, IPersistenceConfig> _pCfgs = new Dictionary<Type, IPersistenceConfig>();
        private readonly LimitsConfig _limits;
        private static readonly ConcurrentDictionary<string, DateTime> _requestTracker = 
            new ConcurrentDictionary<string, DateTime>();

        public GenerateController(IPipelineManager pipelineManager, 
            IConfiguration config, 
            IOptions<AzureConfig> azcfg, 
            IOptions<StreamConfig> stCfg,
            IOptions<FilesystemConfig> fsCfg,
            IOptions<LimitsConfig> limits)
        {
            _pipelineManager = pipelineManager;
            azcfg.Value.Storage.BindConfiguration(config);
            stCfg.Value.BindConfiguration(config);

            _pCfgs[typeof(AzureConfig)] = azcfg.Value;
            _pCfgs[typeof(StreamConfig)] = stCfg.Value;
            _pCfgs[typeof(FilesystemConfig)] = fsCfg.Value;

            _limits = limits.Value;
        }

        private string BuildUrl(string apiCall, params object[] postfix)
        {
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{apiCall}/{string.Join("/", postfix)}";
        }

        /// <summary>The response to a generation request.</summary>
        [Serializable]
        public struct GenerateResponse
        {
            /// <summary>The overall status</summary>
            public string Status;
            /// <summary>The error (if status == "error")</summary>
            public string Error;
            /// <summary>The job Id</summary>
            public Guid Id;
            /// <summary>The opaque queue token for job control</summary>
            public object QueueToken;
            /// <summary>A convenience link for querying job status information</summary>
            public string StatusUrl;
            /// <summary>A convenience to canceling the job</summary>
            public string CancelUrl;
            /// <summary>The generation options used for this job</summary>
            public GeneratorConfig GeneratorOptions;
        }

        private GenerateResponse StartGeneration(GeneratorConfig useOpts, 
            bool trackAfterCompletion = false,
            Action<PipelineBase> completionHandler = null)
        {
            var initiator = new PipelineRequestTracker()
            {
                RemoteAddr = HttpContext.Connection.RemoteIpAddress.ToString(),
                Request = $"{Request.Method} {Request.Scheme}://{Request.Host}{Request.Path}{(Request.Method == "GET" ? $"?{Request.QueryString}" : "")}"
            };

            var limitFailures = new List<(string, string)>();

            if (_limits.MaxRecordCountPerJob.HasValue && 
                useOpts.Count > _limits.MaxRecordCountPerJob.Value)
                limitFailures.Add(($"The value {useOpts.Count} exceeds the allowed limit", "count"));

            if (_limits.MaxQueuedJobs.HasValue &&
                _pipelineManager.Info().Count >= _limits.MaxQueuedJobs.Value)
                limitFailures.Add(("Too many jobs are currently executing, this job cannot be queued", "MaxQueuedJobs"));

            if (_limits.MaxTotalRecordsInFlight.HasValue &&
                _pipelineManager.Info().InFlightCount + useOpts.Count > 
                    _limits.MaxTotalRecordsInFlight.Value)
                limitFailures.Add(("Too many records are currently being generated, this job cannot be queued", "MaxTotalRecordsInFlight"));

            if (_limits.MaxJobCreationRequestRate.HasValue &&
                _requestTracker.ContainsKey(initiator.RemoteAddr) &&
                DateTime.UtcNow.Subtract(_requestTracker[initiator.RemoteAddr]) < 
                    TimeSpan.FromSeconds(_limits.MaxJobCreationRequestRate.Value))
                limitFailures.Add(("The client is requesting job creation too frequently", "MaxJobCreationRequestRate"));

            if (limitFailures.Count > 0)
                throw new AggregateException(limitFailures.Select(
                    lfTuple => new ArgumentException(lfTuple.Item1, lfTuple.Item2)));

            var pipe = new Pipeline();

            if (useOpts.PersistenceConfig == null || useOpts.PersistenceConfig.Count == 0)
                useOpts.PersistenceConfig = _pCfgs;

            if (pipe.Configure(useOpts))
            {
                if (completionHandler != null)
                    pipe.CompletionHandler = completionHandler;

                var queueToken = _pipelineManager.Queue(pipe, initiator, trackAfterCompletion);
                _requestTracker[initiator.RemoteAddr] = DateTime.UtcNow;

                return new GenerateResponse() {
                    Status = PipelineBase.PipelineStatusNames[pipe.Status],
                    Id = pipe.Id,
                    QueueToken = queueToken,
                    StatusUrl = BuildUrl("status", pipe.Id),
                    CancelUrl = BuildUrl("cancel", pipe.Id, queueToken),
                    GeneratorOptions = useOpts
                };
            }

            return new GenerateResponse() {
                Status = "error",
                Error = "Pipeline configuration failed"
            };
        }

        /// <summary>Generate random data according to the specified configuration</summary>
        /// <param name="config">
        /// The job configuration, a default template of which is always available <a href="/info/defaults" target="_blank">here</a>.
        /// </param>
        /// <response code="200">
        /// Metadata about the queued job in the form of a <code>GenerateResponse</code>
        /// object: if the <code>Status</code> property is "error", the request was denied
        /// and the <code>Error</code> property will contain more detailed failure information.
        /// </response>
        [HttpPost]
        [ProducesResponseType(typeof(GenerateResponse), 200)]
        [Produces("application/json")]
        [Consumes("application/json")]
        public ActionResult Post([FromBody] GeneratorConfig config)
        {
            var exDict = new Dictionary<string, string[]>() {{ "validationErrors", new string[] { "Unknown error" }}};

            try
            {
                return new JsonResult(StartGeneration(config, true));
            }
            catch (ArgumentException e)
            {
                exDict = new Dictionary<string, string[]>(){{ e.ParamName ?? "validationErrors", new string[] { e.Message }}};
            }
            catch (AggregateException ae)
            {
                exDict = ae.InnerExceptions
                    .Where(ie => ie is ArgumentException)
                    .ToDictionary(ks => ((ArgumentException)ks).ParamName ?? "unknownParameter", 
                        vs => new string[] { vs.Message });
            }

            return ValidationProblem(new ValidationProblemDetails(exDict));
        }

        /// <summary>
        /// Convenience method to generate &amp; immediately return small data sets
        /// </summary>
        /// <remarks>
        /// This call will <b>block</b> during record generation &amp; accordingly is governed by two limits:
        /// <a href="/info/limits/maxgetgeneraterecordcountperjob" target="_blank"><code>MaxGETGenerateRecordCountPerJob</code>
        /// </a>, the maximum allowable value of the <code>count</code> configuration parameter; 
        /// and <a href="/info/limits/maxgetgenerateruntime" target="_blank"><code>MaxGETGenerateRunTime</code>
        /// </a>, the maximum amount of time (in seconds) the job is allowed to run.
        /// If the latter is exceeded, the job will be summarily canceled and no content will be returned.
        /// </remarks>
        /// <param name="spec">
        /// The specification type, one of the supported
        /// specifications found in the list returned by 
        /// <a href="/info/supported/specifications" target="_blank">
        /// <code>supported/specifications</code></a>
        /// </param>
        /// <param name="count">The number of records to generate, up to
        /// <a href="/info/limits/maxgetgeneraterecordcountperjob" target="_blank">
        /// <code>MaxGETGenerateRecordCountPerJob</code></a>
        /// if specified, and of course all other limits still apply.
        /// </param>
        /// <param name="outputType">The format in which to render the data, one of
        /// the supported types returned by 
        /// <a href="/info/supported/outputs" target="_blank"><code>supported/outputs</code></a>
        /// </param>
        /// <response code="200">
        /// The generated data in the requested output format
        /// </response>
        /// <response code="400">
        /// If the generation takes longer than one minute or otherwise fails, the job will automatically 
        /// cancel and this result will be returned (hopefully including a descriptive error message).
        /// </response>
        [HttpGet("{spec}/{count}.{outputType}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(BadRequestResult), 400)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public ActionResult Get(string spec, uint count, string outputType)
        {
            var gCfg = new GeneratorConfig()
            {
                Count = count,
                Specification = spec,
                OutputFormat = outputType,
                PersistenceConfig = new Dictionary<Type, IPersistenceConfig>()
                {
                    { typeof(FilesystemConfig), new FilesystemConfig() {
                        PersistDirectory = Path.GetTempPath()
                    }}
                }
            };

            var exDict = new Dictionary<string, string[]>() {{ "validationErrors", new string[] { "Unknown error" }}};
            try
            {
                if (_limits.MaxGETGenerateRecordCountPerJob.HasValue &&
                    gCfg.Count > _limits.MaxGETGenerateRecordCountPerJob)
                    throw new ArgumentException($"The value {gCfg.Count} exceeds the allowed limit", "count");

                // TODO: this doesn't belong here! it's a service config error, not a client input error!
                if (_limits.MaxGETGenerateRunTime < 60)
                    throw new ArgumentException("MaxGETGenerateRunTime cannot less than 60");

                var jobWaitMutex = new Semaphore(0, 1);
                List<PersistenceStageResult> results = null;
                var rv = StartGeneration(gCfg, false, (p) =>
                {
                    results = p.Results;
                    jobWaitMutex.Release();
                });

                if (!jobWaitMutex.WaitOne(new TimeSpan(0, 0, (int)_limits.MaxGETGenerateRunTime)) && results == null)
                {
                    _pipelineManager.Cancel((Guid)rv.Id, rv.QueueToken, 
                        new PipelineRequestTracker()
                        {
                            RemoteAddr = "self",
                            Request = "get-simple timeout"
                        });
                }
                else 
                {
                    var fPath = (string)results[0].Meta["path"];
                    byte[] fContents = System.IO.File.ReadAllBytes(fPath);
                    System.IO.File.Delete(fPath);

                    // could use PhysicalFileResult here maybe?
                    var fsr = File(fContents, $"text/{outputType}");
                    
                    if (outputType == "csv")
                        fsr.FileDownloadName = Path.GetFileName((string)results[0].Meta["path"]);

                    return fsr;
                }

                return BadRequest("Timeout");
            }
            catch (ArgumentException ae)
            {
                exDict = new Dictionary<string, string[]>(){ { ae.ParamName ?? "validationErrors", new string[] { ae.Message } } };
            }
            catch (AggregateException ae)
            {
                exDict = ae.InnerExceptions
                    .Where(ie => ie is ArgumentException)
                    .ToDictionary(ks => ((ArgumentException)ks).ParamName ?? "unknownParameter", 
                        vs => new string[] { vs.Message });
            }

            return ValidationProblem(new ValidationProblemDetails(exDict));
        }
    }
}
