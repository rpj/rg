using System;
using System.Collections.Generic;
using System.Linq;
using Roentgenium.Config;
using Roentgenium.Stages.Persistence;

namespace Roentgenium.Interfaces
{
    public abstract class PipelineStageBase : IPipelineStage
    {
        protected readonly GeneratorConfig _genConfig;
        protected PipelineStageBase(GeneratorConfig gCfg)
        {
            _genConfig = gCfg;
        }
    }

    public abstract class PipelineBase : IPipeline
    {
        protected ISourceStage _source;
        protected List<IIntermediateStage> _filters;
        protected List<ISinkStage> _sinks;
        protected List<PersistenceStageBase> _persisters;
        protected GeneratorConfig _config;
        protected List<Type> _specs;

        public enum PipelineStatus
        {
            Invalid = -2,
            Uninitialized = -1,
            Pending = 0,
            Executing,
            Persisting,
            Success,
            Canceled,
            Failed
        };

        public static readonly Dictionary<PipelineStatus, string> PipelineStatusNames =
            new Dictionary<PipelineStatus, string>()
        {
            { PipelineStatus.Invalid, "Invalid" },
            { PipelineStatus.Uninitialized, "Uninitialized" },
            { PipelineStatus.Pending, "Pending" },
            { PipelineStatus.Executing, "Executing" },
            { PipelineStatus.Persisting, "Persisting" },
            { PipelineStatus.Success, "Complete" },
            { PipelineStatus.Canceled, "Canceled" },
            { PipelineStatus.Failed, "Failed" },
        };

        public PipelineStatus Status { get; protected set; } = PipelineStatus.Uninitialized;
        public readonly Guid Id = Guid.NewGuid();
        public List<string> Specifications { get => _specs.Select(s => s.Name.Replace("Specification", "")).ToList(); }
        public List<SinkStageArtifact> Artifacts { get; protected set; }
        public List<PersistenceStageResult> Results { get; protected set; }
        public Action<PipelineBase> CompletionHandler { get; set; }

        public abstract TimeSpan Elapsed { get; }
        public abstract decimal Progress { get; }
        public abstract uint RecordCount { get; }
        public abstract uint CurrentCount { get; }
        public abstract bool Configure(GeneratorConfig config);
        public abstract bool Execute();
        public abstract bool Cancel();
    }

    [Serializable]
    public class PipelineInfo
    {
        public Guid Id;
        public DateTime Created;
        public DateTime? Completed;
        public string Status;
        public TimeSpan Elapsed;
        public decimal GenerationProgress;
        public uint RecordCount;
        public PipelineRequestTracker? Initiator;
        public PipelineRequestTracker? Cancelor;
        public TimeSpan? Age;
        [NonSerialized] public PipelineBase.PipelineStatus TypedStatus;
    }

    [Serializable]
    public class PipelineManagerLifetime
    {
        public uint Jobs;
        public uint Count;
        public TimeSpan GenerationTime;
        public TimeSpan Uptime;
        public Dictionary<string, int> SpecFreq = new Dictionary<string, int>();
        public Dictionary<string, int> ResultFreq = new Dictionary<string, int>();
        public Dictionary<string, int> OutputFreq = new Dictionary<string, int>();
    }

    [Serializable]
    public class PipelineManagerInfo
    {
        public uint Count;
        public uint InFlightCount;
        public readonly List<PipelineInfo> List = new List<PipelineInfo>();
    }

}
