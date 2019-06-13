using System;
using System.IO;

namespace Roentgenium.Interfaces
{
    /// <summary>
    /// The artifact record produced by sink stages, for input to
    /// persistence stages. Sink stages must ensure that *at least*
    /// the Name property is set to a contextually-relevant (to
    /// any persist stage that may follow) and valid URI pointing to the
    /// stage artifact's binary data *or* a Stream providing
    /// said data directly. It can - and should! - provide both.
    /// </summary>
    public class SinkStageArtifact
    {
        public Guid Id;
        public string Name;
        public string Type;
        [NonSerialized] public Stream ByteStream;

        public SinkStageArtifact(IArtifactCreator creator)
        {
            Type = creator.ToString();
        }

        public void Cleanup()
        {
            try
            {
                // force disposal of the .ByteStream before removing the file,
                // as if .Name is in fact a file path then .ByteStream is likely
                // to hold a lock on that file
                ByteStream.Dispose();
                ByteStream = null;

                // use GetFullPath to validate that .Name is an actual path,
                // as sinks aren't required to set .Name to a filesystem path
                File.Delete(Path.GetFullPath(Name));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Artifact cleanup for ({Id}, '{Name}') failed: {e}");
            }
        }
    }

}
