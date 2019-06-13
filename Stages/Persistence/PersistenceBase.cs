using Roentgenium.Config;
using Roentgenium.Interfaces;
using System;
using System.Collections.Generic;

namespace Roentgenium.Stages.Persistence
{
    public enum PersistenceStatus
    {
        Invalid = -1,
        Configured,
        Compressing,
        Uploading,
        Encrypting,
        CleaningUp,
        Success,
        Errored
    }

    public class PersistenceStageBase : PipelineStageBase, IPersistenceStage
    {
        public PersistenceStatus Status { get; protected set; } = PersistenceStatus.Invalid;

        public Guid Id { get; protected set; } = new Guid();

        public DateTime Timestamp { get; protected set; } = DateTime.UnixEpoch;

        public string ArtifactName { get; protected set; }

        public PersistenceStageBase(GeneratorConfig gCfg) : base(gCfg) { }

        public override string ToString()
        {
            return GetType().Name;
        }

        // Returned format:
        // [userPrefixIfSet-][specificationChars][recordCount]-[first4configGuid]-[timestamp]-[first4artifactGuid].[outputTypeExtenstion]
        // |----------- static for all artifacts in a pipeline -----------| |-------------- dynamic per-artifact -----------------|
        private string FinalArtifactName(GeneratorConfig gCfg, SinkStageArtifact prevArt)
        {
            return
                // [userPrefixIfSet-]
                ((string.IsNullOrEmpty(gCfg.UserPrefix) ?
                    "" : $"{gCfg.UserPrefix}-") +
                // [specificationChars][recordCount]
                $"{gCfg.Specification.ToString().ToUpper().Substring(0, 3)}{gCfg.Count}" +
                // -[first4configGuid]
                $"-{gCfg.Id.ToString("N").Substring(0, 4)}" +
                // -[timestamp]-
                $"-{Timestamp:MMddyyTHHmmZ}-" +
                // [first4artifactGuid]
                $"{prevArt.Id.ToString("N").Substring(0, 4)}").ToUpper() +
                // .[outputTypeExtenstion]
                $".{prevArt.Type}";
        }

        public virtual PersistenceStageResult Persist(SinkStageArtifact sa, Dictionary<string, object> extraMeta = null)
        {
            if (sa != null && sa.ByteStream != null && sa.ByteStream.CanRead && sa.ByteStream.CanSeek)
            {
                Id = Guid.NewGuid();
                Timestamp = DateTime.UnixEpoch;
                ArtifactName = FinalArtifactName(_genConfig, sa);
                return new PersistenceStageResult();
            }

            return null;
        }
    }
}
