using Roentgenium.Config;
using Roentgenium.Stages.Persistence;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Roentgenium.Interfaces
{
    /// <summary>
    /// A class that generates values for a field. Options is of type:
    /// <code>FieldGeneratorOptions = Dictionary&lt;FieldGeneratorOptionType, object&gt;</code>
    /// </summary>
    public interface IFieldGenerator
    {
        object GenerateField(ref FieldGeneratorOptions options);
    }
    
    /// <summary>
    /// A class that links the value of one field to another in the same specification.
    /// </summary>
    public interface IFieldLinker
    {
        object LinkField(object linkedFieldValue, ref FieldGeneratorOptions options);
    }

    public interface IPersistenceConfig { }

    /// <summary>
    /// The data-transform pipeline interface.
    /// 
    /// Flow through the stages:
    /// 
    ///     Source -> [Intermediate 1..N] -> [Sink 1..N] -> [Persist 1..N]
    ///             +--------- transform phase -----------+
    /// 
    /// Notes:
    /// * Only a single source is ever allowed. (TODO: make config'able!)
    /// * Records are mutable during the transform phase &amp; immutable afterwards (in actuality, 
    ///   the records become irrelevant after the transform phase).
    /// * Intermediate, sink &amp; persist stages will be executed in the order in which they
    ///   are configured. However, this is only pertinent to Intermediate stages,
    ///   as the result of the prior stage is used as input to the next.
    /// </summary>
    public interface IPipeline
    {
        /// <summary>
        /// Configures the pipeline for execution. Must be called prior to calling Execute().
        /// </summary>
        /// <param name="config">The proposed generator configuration</param>
        /// <returns>true if configured successfully, false (or exception) otherwise</returns>
        /// <throws>ArgumentException</throws>
        /// <throws>NotImplementedException</throws>
        bool Configure(GeneratorConfig config);

        /// <summary>
        /// Execute the pipeline, based on the configuration given at the prior Configure() call.
        /// </summary>
        /// <returns>false if not able to execute (unconfigured) or other failure, true on success</returns>
        bool Execute();

        /// <summary>
        /// Cancel the pipeline.
        /// </summary>
        bool Cancel();
    }

    /// <summary>
    /// A 'tag' interface to mark all pipeline stages.
    /// </summary>
    public interface IPipelineStage { }

    /// <summary>
    /// A 'tag' interface to mark all generated record types (may be / often is paired with ISpecification).
    /// </summary>
    public interface IGeneratedRecord { }

    /// <summary>
    /// A 'tag' interface to mark all records specifications.
    /// </summary>
    public interface ISpecification : IGeneratedRecord { }

    /// <summary>
    /// A source stage in the pipeline. Only one of these can exist
    /// in a given pipeline.
    /// </summary>
    public interface ISourceStage : IPipelineStage
    {
        /// <summary>
        /// Produce the next record. All source stages are assumed (and concrete
        /// implementions should be implemented to be) capable of producing infinite
        /// records, e.g. Next() should never return an "invalid" result. In practice,
        /// sources should be able to provide at least N distinct (not necessarily unique,
        /// as duplicates may be desirable and/or inherent to the data set) records
        /// where N is the largest GeneratorConfig.Count value a user ever request.
        /// </summary>
        /// <param name="spec">The ISpecification-implementing field spec</param>
        /// <param name="seqNo">The current monotonically-increasing sequence number.</param>
        /// <returns>The next new record.</returns>
        IGeneratedRecord Next(Type spec, uint seqNo);
    }

    public interface IArtifactCreator { }

    /// <summary>
    /// A sink stage in the pipeline. Sink stages are the final transform
    /// allowed in the pipeline; post sink-stage execution, the data is
    /// considered read-only.
    /// </summary>
    public interface ISinkStage : IPipelineStage, IArtifactCreator
    {
        /// <summary>
        /// Sink a record to this stage.
        /// </summary>
        /// <param name="record">The record to sink.</param>
        /// <returns>'true' on success</returns>
        bool Sink(IGeneratedRecord record);

        /// <summary>
        /// Prepares the stage to recieve records. Guaranteed to be called before
        /// the first call to Sink() is made, and the return value of this method
        /// determines if Sink() is called on this instance ('true' for yes).
        /// </summary>
        /// <returns>'true' to continue to call Sink() on this instance</returns>
        bool Prepare();

        /// <summary>
        /// Finish's the stage. Guaranteed to be called after final Sink() call is made.
        /// </summary>
        /// <returns>The stage artifact</returns>
        SinkStageArtifact Finish();
    }

    /// <summary>
    /// An intermediate pipeline stage (a.k.a. 'filter'), which are in reality both 
    /// a source and sink stage. The pipeline will pass records through intermediate 
    /// stages by calling Sink() followed immediately by Next(). The sequence number 
    /// remains constant throughout one execution of the intermediate stages of a pipeline.
    /// </summary>
    public interface IIntermediateStage : ISourceStage, ISinkStage { }

    /// <summary>
    /// A persistence stage in the pipeline. These stages cannot transform data,
    /// only persist it somewhere else. They are given as input SinkStageArtifact objects
    /// which sink stages must guarantee have a way to retrieve the binary data of
    /// the stage's resulting artifact.
    /// </summary>
    public interface IPersistenceStage : IPipelineStage, IArtifactCreator
    {
        PersistenceStageResult Persist(SinkStageArtifact sinkArt, Dictionary<string, object> extraMeta = null);
    }

    public interface IPipelineManager
    {
        object Queue(PipelineBase pipeline, PipelineRequestTracker? initiator = null, bool trackAfterCompletion = false);
        bool Cancel(Guid pId, object token, PipelineRequestTracker? cancelor = null);
        PipelineBase.PipelineStatus GetStatus(Guid pId);
        List<PersistenceStageResult> GetResults(Guid pId);
        TimeSpan GetElapsed(Guid pId);
        decimal GetProgress(Guid pId);
        Dictionary<Type, PersistenceStatus> GetPersistenceStatus(Guid pId);
        PipelineManagerInfo Info();
        PipelineManagerLifetime Lifetime();
        void Remove(Guid pId);
    }
    
    public interface IKeyVault
    {
        void AddKeyVaultToBuilder(IConfigurationBuilder config);
    }

    public enum SpecificationType
    {
        CensusData = 0,
        PlayerProfile,
        Names,
        Companies,
        Addresses,
        ContactInfo,
        Passwords
    }

    public enum FieldGeneratorOptionType
    {
        NoOpt_FirstValue = -1,
        MinValue,
        MaxValue,
        BlankFrequency,
        LengthLimit,
        RoundTo,
        IsNumeric,
        AllowUnsafeChars,
        Variant,
        FormatString,
        NoOpt_LastValue
    }

    public class FieldGeneratorOptions
    {
        public static readonly int BlankFrequencyDisabled = 0; // 0 == never allow blanks
        public static readonly int DefaultDecimalPlaceRound = 2;
        public static readonly int DefaultLength = 10;

        public double MinValue          = 0;
        public double MaxValue          = int.MaxValue;
        public int BlankFrequency       = BlankFrequencyDisabled;
        public int LengthLimit          = DefaultLength;
        public int RoundTo              = DefaultDecimalPlaceRound;
        public bool IsNumeric           = false;
        public bool AllowUnsafeChars    = false;
        public string Variant           = null;
        public string FormatString      = null;

        public GeneratorConfig Config   = null;
    }

    /// <summary>
    /// The result of a persistence stage, generally user-facing information relating
    /// to - at very least - how they'll retrieve/access/track/make-use-of the persisted data.
    /// </summary>
    public class PersistenceStageResult
    {
        public Guid Id;
        public DateTime Timestamp;
        public Guid PipelineId;
        public bool Success;
        public Dictionary<string, object> Meta;
    }
}