using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Roentgenium.Config;
using Roentgenium.Interfaces;
using Roentgenium.Attributes;
using Roentgenium.Stages.Sinks;
using Roentgenium.Stages.Sources;
using Roentgenium.FieldGenerators;
using Roentgenium.Stages.Persistence;

namespace Roentgenium
{
    public class Pipeline : PipelineBase
    {
        private DateTime? _start;
        private DateTime? _end;
        private uint _seqNo;
        private bool _canceled = false;

        public override TimeSpan Elapsed
        {
            get
            {
                if (!_start.HasValue)
                    return TimeSpan.MinValue;

                if (!_end.HasValue)
                    return DateTime.UtcNow.Subtract(_start.Value);

                return (TimeSpan)_end?.Subtract((DateTime)_start);
            }
        }

        public override decimal Progress => Status == PipelineStatus.Executing 
            ? ((decimal)_seqNo / _config.Count) * 100 : -1;

        public override uint RecordCount => _config.Count;

        public override uint CurrentCount => _seqNo;

        public override string ToString() { return $"Pipeline<{Id}>"; }

        public override bool Configure(GeneratorConfig config)
        {
            if (Status != PipelineStatus.Uninitialized)
                return false;

            Console.WriteLine($"{this}: Configuring pipeline with {config}...");
            _config = config;

            _specs = new List<Type>();
            _sinks = new List<ISinkStage>();

            // find all ISpecification-implementing types for the configured specification
            BuiltIns.SpecTypes.ForEach(sit =>
            {
                sit.GetCustomAttributes(false)
                    .Where(ca => ca.GetType() == typeof(SpecificationAttribute)).ToList()
                    .ForEach(ca =>
                    {
                        var specCa = (SpecificationAttribute)ca;
                        if (specCa.SpecType.ToString() == _config.Specification)
                        {
                            _config.TypedSpecification = sit;
                            _specs.Add(sit);
                        }
                    });
            });

            if (!_specs.Any())
                throw new ArgumentException($"Specification '{_config.Specification}' is not valid");

            if (_specs.Count > 1)
                throw new ArgumentException("Multi-spec generation is not yet implemented");

            var ofLc = _config.OutputFormat.ToLower();

            if (!BuiltIns.OutputSinks.ContainsKey(ofLc))
                throw new ArgumentException($"Invalid output format '{ofLc}', valid formats are: {string.Join(',', BuiltIns.OutputSinks.Keys)}");

            try 
            {
                _sinks.Add((ISinkStage)Activator.CreateInstance(BuiltIns.OutputSinks[ofLc], _config));
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException is ArgumentException ? tie.InnerException : tie;
            }

            // TODO: probably remove this?
            if (ofLc == "stream")
                _sinks.Add(new CsvSink(_config));

            _filters = new List<IIntermediateStage>(_config.Filters.Count);
            _config.Filters.ForEach(ciFilterCfg =>
            {
                if (!BuiltIns.Filters.ContainsKey(ciFilterCfg))
                    throw new ArgumentException($"Invalid configuration for filter '{ciFilterCfg}");

                _filters.Add((IIntermediateStage)Activator.CreateInstance(BuiltIns.Filters[ciFilterCfg]));
            });

            _persisters = new List<PersistenceStageBase>();
            BuiltIns.PersistenceStages.ForEach(pStageType =>
            {
                var newStage = (PersistenceStageBase)Activator.CreateInstance(pStageType, _config);
                if (newStage.Status == PersistenceStatus.Configured)
                    _persisters.Add(newStage);
            });

            _config.Id = Id;
            Status = PipelineStatus.Pending;
            Console.WriteLine($"{this}: configured successfully.");
            return true;
        }

        public override bool Execute()
        {
            if (Status != PipelineStatus.Pending)
                return false;

            _start = DateTime.UtcNow;
            Console.WriteLine($"{this}: Executing pipeline {Id} at {_start}");
            Status = PipelineStatus.Executing;
            Artifacts = new List<SinkStageArtifact>();
            Results = new List<PersistenceStageResult>();

            GeneratorsStatic.ThreadInit();
            _source = new GeneratorSource(ref _config);

            // One day, we'll support multi-spec generation...
            Type iSpec = _specs.First();
            var transformStageSw = new Stopwatch();
            transformStageSw.Start();

            // for each spec, run the pipeline once for each record
            // to be generated
            _sinks.ForEach(sink => sink.Prepare());
            for (_seqNo = 0; !_canceled && _seqNo < _config.Count; _seqNo++)
            {
                // source stage
                IGeneratedRecord nextSrc;
                try
                {
                    nextSrc = _source.Next(iSpec, _seqNo);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{this}: Source-stage failure at seqNo={_seqNo} of {_config.Count}:\n{e}");
                    continue;
                }

                // intermediate stage
                _filters.ForEach(filter =>
                {
                    if (filter.Prepare() && filter.Sink(nextSrc))
                        nextSrc = filter.Next(iSpec, _seqNo);
                    filter.Finish();
                });

                // sink stage
                _sinks.ForEach(sink => sink.Sink(nextSrc));
            }

            transformStageSw.Stop();
            Console.WriteLine($"{this}: Transform stage took {transformStageSw.Elapsed}");

            if (!_canceled)
            {
                // persist stage
                var persistStageSw = new Stopwatch();
                persistStageSw.Start();
                Status = PipelineStatus.Persisting;

                // capture elapsed time to this point for persisters' use
                var extraMeta = new Dictionary<string, object>()
                {
                    { "tranformStageElapsed", Elapsed }
                };

                _sinks.ForEach(sink =>
                {
                    var artifact = sink.Finish();
                    if (artifact != null)
                    {
                        Artifacts.Add(artifact);
                        _persisters.ForEach(p =>
                        {
                            Results.Add(p.Persist(artifact, extraMeta));
                            if (artifact.ByteStream != null && artifact.ByteStream.CanSeek)
                                artifact.ByteStream.Seek(0, SeekOrigin.Begin);
                        });
                    }
                });

                Artifacts.ForEach(a => a.Cleanup());
                persistStageSw.Stop();
                Console.WriteLine($"{this}: Persist stage took {persistStageSw.Elapsed}");
            }
            else
                _sinks.Select(sink => sink.Finish()).ToList().ForEach(art => art?.Cleanup());

            Status = _canceled ? PipelineStatus.Canceled : 
                (_seqNo < _config.Count ? PipelineStatus.Failed : PipelineStatus.Success);
            _end = DateTime.UtcNow;

            if (CompletionHandler != null)
                CompletionHandler(this);

            return Status == PipelineStatus.Success;
        }

        public override bool Cancel()
        {
            if (_canceled)
                return false;

            Console.WriteLine($"{this}: Canceling pipeline {Id} at {_seqNo} of {_config.Count} records");
            return _canceled = true;
        }
    }
}
