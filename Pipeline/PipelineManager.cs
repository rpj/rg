using Roentgenium.Config;
using Roentgenium.Interfaces;
using Roentgenium.Stages.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Roentgenium
{
    using CompletionHandler = Action<PipelineBase>;

    [Serializable]
    public struct PipelineRequestTracker
    {
        public string RemoteAddr;
        public string Request;
    }

    public class PipelineManager : IPipelineManager
    {
        [Serializable]
        private class PipelineTracker
        {
            [NonSerialized] public PipelineBase Pipeline;
            public PipelineRequestTracker? Initiator;
            public PipelineRequestTracker? Cancelor;
            public Task Task;
            public DateTime Created;
            public DateTime? Completed;
            public DateTime? LastAccessed;
            public object QueueToken;
            [NonSerialized] public CompletionHandler OriginalCompletionHandler;
            public bool TrackAfterCompletion = false;
        }

        private readonly Mutex _pipelinesMutex = new Mutex();
        private readonly Dictionary<Guid, PipelineTracker> _pipelines = 
            new Dictionary<Guid, PipelineTracker>();
        private readonly ConnectionMultiplexer _streamConn = null;
        private Dictionary<Guid, Guid> _idResolverCache = null;
        private readonly LimitsConfig _limits = null;
        private PipelineManagerLifetime _lifetimeStats = new PipelineManagerLifetime();
        private readonly DateTime _birthDay = DateTime.UtcNow;

        public PipelineManagerLifetime Lifetime()
        {
            return _lifetimeStats;
        }

        protected delegate bool LockingGetterCond();

        public PipelineManager(IConfiguration config, 
            IOptions<StreamConfig> streamCfg,
            IOptions<LimitsConfig> limits)
        {
            streamCfg.Value.BindConfiguration(config);

            if (!string.IsNullOrEmpty(streamCfg.Value.ConnectionString))
            {
                try 
                {
                    _streamConn = ConnectionMultiplexer.Connect(streamCfg.Value.ConnectionString);
                    _idResolverCache = new Dictionary<Guid, Guid>();
                }
                catch (RedisConnectionException rde)
                {
                    Console.WriteLine($"Unable to connect to stream service, no ID resolver availble. Msg: {rde.Message}");
                }
            }

            _limits = limits.Value;
        }

        protected Guid ResolveId(Guid id)
        {
            if (_streamConn != null && _idResolverCache != null)
            {
                if (!_idResolverCache.ContainsKey(id))
                {
                    var pId = _streamConn.GetDatabase().HashGet(id.ToString(), "pipelineId");
                    _idResolverCache[id] = pId.HasValue ? new Guid(pId.ToString()) : id;
                }

                return _idResolverCache[id];
            }

            return id;
        }

        // extraCond will be called from *inside* the critical section
        protected T LockingGetter<T>(Guid pId, 
            string propName,
            LockingGetterCond extraCond = null, 
            bool markAsAccessed = false)
        {
            T retVal = default(T);
            pId = ResolveId(pId);

            _pipelinesMutex.WaitOne();
            if (_pipelines.ContainsKey(pId) && (extraCond == null || extraCond()))
            {
                if (propName == null)
                {
                    retVal = (T)(object)_pipelines[pId];
                }
                else
                {
                    var prop = _pipelines[pId].Pipeline.GetType().GetProperty(propName);

                    if (prop != null && prop.PropertyType == typeof(T))
                    {
                        if (markAsAccessed)
                            _pipelines[pId].LastAccessed = DateTime.UtcNow;
                        retVal = (T)prop.GetValue(_pipelines[pId].Pipeline);
                    }
                }
            }
            _pipelinesMutex.ReleaseMutex();

            return retVal;
        }

        // [1] Use TaskCreationOptions.LongRunning here not because the consituent operations are 
        // coarse-grained (they aren't and could in fact benefit from parallelization in future iterations),
        // but because this hint informs the scheduler not to schedule the task on the local queue
        // which - given that this method is called from an HTTP-request-handling thread - would 
        // schedule it on the same work queue that future HTTP requests would be serviced from.
        // Further information here: https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=netframework-4.8#long-running-tasks
        public object Queue(PipelineBase pipeline, 
            PipelineRequestTracker? initiator, 
            bool trackAfterCompletion = false)
        {
            _pipelinesMutex.WaitOne();
            if (_pipelines.ContainsKey(pipeline.Id))
                return null;

            if (pipeline.Status < PipelineBase.PipelineStatus.Pending)
                return null;

            var tracker = new PipelineTracker()
            {
                Pipeline = pipeline,
                Initiator = initiator,
                Created = DateTime.UtcNow,
                LastAccessed = null,
                QueueToken = Guid.NewGuid().ToString(),
                OriginalCompletionHandler = pipeline.CompletionHandler,
                TrackAfterCompletion = trackAfterCompletion
            };

            pipeline.CompletionHandler = OnPipelineStateChance;
            tracker.Task = Task.Factory.StartNew(pipeline.Execute, TaskCreationOptions.LongRunning /* [1] */);
            
            _pipelines[pipeline.Id] = tracker;
            _pipelinesMutex.ReleaseMutex();

            return _pipelines[pipeline.Id].QueueToken;
        }

        public bool Cancel(Guid pId, object token, PipelineRequestTracker? cancelor)
        {
            var rv = false;
            var realId = ResolveId(pId);
            PipelineTracker pipe = null;

            _pipelinesMutex.WaitOne();

            if (_pipelines.ContainsKey(realId) && (pipe = _pipelines[realId]).QueueToken.Equals(token))
                if (rv = pipe.Pipeline.Cancel())
                    pipe.Cancelor = cancelor;

            _pipelinesMutex.ReleaseMutex();

            return rv;
        }

        public PipelineBase.PipelineStatus GetStatus(Guid pId)
        {
            return LockingGetter<PipelineBase.PipelineStatus>(pId, "Status", null, true);
        }

        public List<PersistenceStageResult> GetResults(Guid pId)
        {
            return LockingGetter<List<PersistenceStageResult>>(pId, "Results",
                () => GetStatus(pId) == PipelineBase.PipelineStatus.Success);
        }
        
        public TimeSpan GetElapsed(Guid pId)
        {
            return LockingGetter<TimeSpan>(pId, "Elapsed");
        }
        
        public decimal GetProgress(Guid pId)
        {
            return LockingGetter<decimal>(pId, "Progress");
        }

        public Dictionary<Type, PersistenceStatus> GetPersistenceStatus(Guid pId)
        {
            return LockingGetter<Dictionary<Type, PersistenceStatus>>(pId, "PersistenceStatus");
        }

        public void Remove(Guid pId)
        {
            _pipelinesMutex.WaitOne();
            _pipelines.Remove(pId);
            _pipelinesMutex.ReleaseMutex();
        }
        
        public PipelineManagerInfo Info()
        {
            PipelineManagerInfo rv = null;
            lock(this)
            {
                _lifetimeStats.Uptime = DateTime.UtcNow.Subtract(_birthDay);
                rv = new PipelineManagerInfo()
                {
                    Count = (uint)_pipelines.Count,
                    InFlightCount = 0
                };
            }

            if (_pipelines.Count > 0)
            {
                _pipelinesMutex.WaitOne();
                _pipelines.ToList().ForEach(p =>
                {
                    var pipe = p.Value.Pipeline;
                    if (pipe.Status < PipelineBase.PipelineStatus.Success)
                        rv.InFlightCount += pipe.RecordCount - pipe.CurrentCount;
                        
                    rv.List.Add(new PipelineInfo()
                    {
                        RecordCount = pipe.RecordCount,
                        Elapsed = pipe.Elapsed,
                        Status = PipelineBase.PipelineStatusNames[pipe.Status],
                        TypedStatus = pipe.Status,
                        Id = p.Key,
                        Created = p.Value.Created,
                        Completed = p.Value.Completed,
                        GenerationProgress = pipe.Progress,
                        Initiator = p.Value.Initiator,
                        Cancelor = p.Value.Cancelor,
                        Age = DateTime.UtcNow.Subtract(p.Value.LastAccessed ?? 
                            (p.Value.Completed ?? DateTime.UtcNow))
                    });
                });
                _pipelinesMutex.ReleaseMutex();
            }

            return rv;
        }

        private void OnPipelineStateChance(PipelineBase p)
        {
            var tracker = LockingGetter<PipelineTracker>(p.Id, null);

            tracker.Completed = DateTime.UtcNow;

            if (tracker?.OriginalCompletionHandler != null)
                tracker.OriginalCompletionHandler(p);
            
            lock(this)
            {
                _lifetimeStats.Count += p.RecordCount;
                _lifetimeStats.GenerationTime += p.Elapsed;
                if (!_lifetimeStats.ResultFreq.ContainsKey(p.Status.ToString()))
                    _lifetimeStats.ResultFreq[p.Status.ToString()] = 0;
                p.Specifications.ForEach(s => {
                    if (!_lifetimeStats.SpecFreq.ContainsKey(s))
                        _lifetimeStats.SpecFreq[s] = 0;
                    _lifetimeStats.SpecFreq[s]++;
                });
                p.Artifacts.ForEach(a => {
                    if (!_lifetimeStats.OutputFreq.ContainsKey(a.Type))
                        _lifetimeStats.OutputFreq[a.Type] = 0;
                    _lifetimeStats.OutputFreq[a.Type]++;
                });
                _lifetimeStats.ResultFreq[p.Status.ToString()]++;
                _lifetimeStats.Jobs++;
            }

            if (!tracker.TrackAfterCompletion)
            {
                Remove(p.Id);
            }
        }
    }
}
