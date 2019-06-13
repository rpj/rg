using Roentgenium.Interfaces;
using System;
using System.IO;
using Roentgenium.Config;

namespace Roentgenium.Stages.Sinks
{
    public abstract class SinkStageBase : PipelineStageBase, ISinkStage
    {
        protected string FileName;
        protected StreamWriter Writer;

        protected SinkStageBase(GeneratorConfig gCfg) : base(gCfg) { }

        public override string ToString() { return GetType().Name.Replace("Sink", "").ToLower(); }

        public virtual bool Prepare()
        {
            try 
            {
                FileName = Path.GetTempFileName();
                Writer = new StreamWriter(FileName);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"SinkStageBase unable to initialize temporary file: {e}");
                return false;
            }
        }

        public abstract bool Sink(IGeneratedRecord inRec);

        public virtual SinkStageArtifact Finish()
        {
            Writer.Close();
            return new SinkStageArtifact(this)
            {
                Id = Guid.NewGuid(),
                ByteStream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read),
                Name = FileName
            };
        }
    }
}
