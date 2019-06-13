using System.Linq;
using Roentgenium.Attributes;
using Roentgenium.Config;
using Roentgenium.Interfaces;

namespace Roentgenium.Stages.Sinks
{
    [OutputFormatSinkType]
    public class TxtSink : SinkStageBase
    {
        private int _recCount = 0;

        public TxtSink(GeneratorConfig gCfg) : base(gCfg) { }

        public override bool Sink(IGeneratedRecord inRec)
        {
            Writer.WriteLine($"--- #{++_recCount} ---");
            inRec.GetType().GetProperties().ToList().ForEach(prop =>
                Writer.WriteLine($"{prop.Name}: {prop.GetValue(inRec)}"));
            Writer.WriteLine();
            return true;
        }
    }
}
