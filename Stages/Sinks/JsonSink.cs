using System;
using Roentgenium.Attributes;
using Roentgenium.Config;
using Roentgenium.Interfaces;
using Newtonsoft.Json;

namespace Roentgenium.Stages.Sinks
{
    [OutputFormatSinkType]
    public class JsonSink : SinkStageBase
    {
        private int _recCount;

        public JsonSink(GeneratorConfig gCfg) : base(gCfg) { }

        public override bool Prepare()
        {
            if (!base.Prepare())
                return false;

            _recCount = 0;
            Writer.Write("[");
            return true;
        }

        public override bool Sink(IGeneratedRecord inRec)
        {
            try
            {
                var jsonStr = JsonConvert.SerializeObject(inRec);
                Writer.Write((_recCount++ == 0 ? "" : ",") + jsonStr);
            }
            catch (Exception e)
            {
                Console.WriteLine($"JsonSink.Sink() failed: {e}");
                return false;
            }

            return true;
        }
        
        public override SinkStageArtifact Finish()
        {
            Writer.Write("]");
            return base.Finish();
        }
    }
}
