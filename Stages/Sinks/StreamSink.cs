using Roentgenium.Attributes;
using Roentgenium.Config;
using Roentgenium.Interfaces;
using System;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace Roentgenium.Stages.Sinks
{
    [OutputFormatSinkType]
    public class StreamSink : SinkStageBase
    {
        private readonly ConnectionMultiplexer _cm;
        private readonly IDatabase _activeDb;
        private readonly string _streamId;

        public StreamSink(GeneratorConfig gCfg) : base(gCfg) 
        {
            if (!gCfg.Extra.ContainsKey("streamId") || !(gCfg.Extra["streamId"] is string))
                throw new ArgumentException("StreamSink requires 'streamId' (string) in 'extra' configuration field.");
        
            if ((bool)!gCfg.PersistenceConfig?.ContainsKey(typeof(StreamConfig)))
                throw new ArgumentException("StreamSink is not correctly configured for use.");

            _streamId = (string)gCfg.Extra["streamId"];

            try
            {
                _cm = ConnectionMultiplexer.Connect(((StreamConfig)gCfg.PersistenceConfig[typeof(StreamConfig)]).ConnectionString);
                _activeDb = _cm.GetDatabase(0);
            }
            catch (RedisConnectionException rde)
            {
                throw new ArgumentException($"Incorrect streaming service configuration. Underlying failure message: '{rde.Message}'");
            }

            Console.WriteLine($"StreamSink publishing to streamId={_streamId}");
         }

        public override bool Prepare()
        {
            return base.Prepare();
        }

        public override bool Sink(IGeneratedRecord inRec)
        {
            _activeDb.Publish(_streamId, JsonConvert.SerializeObject(inRec));
            return true;
        }

        public override SinkStageArtifact Finish()
        {
            return null;
        }
    }
}
