using System;
using System.IO;
using System.Collections.Generic;
using Roentgenium.Config;
using Roentgenium.Interfaces;

namespace Roentgenium.Stages.Persistence
{
    public class FilesystemPersistence : PersistenceStageBase
    {
        private FilesystemConfig _config;

        public override string ToString()
        {
            return $"{base.ToString()}<{Path.GetFullPath(_config.PersistDirectory)}>";
        }

        public FilesystemPersistence(GeneratorConfig gCfg)
            : base(gCfg)
        {
            if (_genConfig.PersistenceConfig.ContainsKey(typeof(FilesystemConfig)))
            {
                _config = (FilesystemConfig)_genConfig.PersistenceConfig[typeof(FilesystemConfig)];
                Status = PersistenceStatus.Configured;
                Console.WriteLine($"{this} enabled");
            }
        }

        public override PersistenceStageResult Persist(SinkStageArtifact artifact, Dictionary<string, object> extraMeta = null)
        {
            if (base.Persist(artifact, extraMeta) != null)
            {
                var fullArtifactPath = Path.GetFullPath(Path.Combine(_config.PersistDirectory, ArtifactName));
                using (var fileStream = new FileStream(fullArtifactPath, FileMode.OpenOrCreate))
                {
                    artifact.ByteStream.CopyTo(fileStream);
                    Status = PersistenceStatus.Success;
                    Console.WriteLine($"{this}: {artifact.Id} saved to {fullArtifactPath}");
                    return new PersistenceStageResult()
                    {
                        Id = Id,
                        PipelineId = _genConfig.Id,
                        Timestamp = Timestamp,
                        Success = true,
                        Meta = new Dictionary<string, object>()
                        {
                            { "id", artifact.Id.ToString() },
                            { "specification", _genConfig.Specification.ToString().ToLower() },
                            { "recordCount", _genConfig.Count.ToString() },
                            { "sizeBytes", new FileInfo(fullArtifactPath).Length },
                            { "path", fullArtifactPath }
                        }
                    };
                }
            }

            Status = PersistenceStatus.Errored;
            return new PersistenceStageResult()
            {
                Id = Id,
                Timestamp = Timestamp,
                PipelineId = _genConfig.Id,
                Success = false,
                Meta = null
            };
        }
    }
}
