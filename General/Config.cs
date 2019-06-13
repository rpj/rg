using Roentgenium.Interfaces;
using Roentgenium.Specifications;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Roentgenium.Config
{
    /// <summary>Generation job configuration</summary>
    [Serializable]
    public class GeneratorConfig
    {
        public override string ToString()
        {
            return $"GeneratorConfig<Count={Count} Output={OutputFormat} Spec={Specification}>";
        }

        [NonSerialized] private static readonly int UserPrefixMaxLength = 32;
        [NonSerialized] private static readonly Regex UserPrefixReplaceRegex = new Regex("[^a-zA-Z0-9]");

        [NonSerialized] public Guid Id = Guid.Empty;

        /// <summary>
        /// The number of records of the Specification type to generate,
        /// subject to any applicable limits.
        /// </summary>
        public uint Count { get; set; } = 100;

        /// <summary>
        /// The output format to render the data into; must be one of the supported
        /// formats returned by <a href="/info/supported/outputs" target="_blank"><code>supported/outputs</code></a>.
        /// </summary>
        public string OutputFormat { get; set; } = "csv";

        /// <summary>
        /// The specification to be generated, one of <a href="/info/supported/specifications" target="_blank">
        /// <code>supported/specifications</code></a>.
        /// </summary>
        public string Specification { get; set; } = SpecificationType.CensusData.ToString();

        [NonSerialized] public Type TypedSpecification = typeof(CensusDataSpecification);

        /// <summary>
        /// The filters to use during generation, may be empty. 
        /// Those specified must be one of <a href="/info/supported/filters" target="_blank">
        /// <code>supported/filters</code></a>.
        /// </summary>
        public List<string> Filters { get; set; } = new List<string>();

        /// <summary>
        /// Any extra key/value pairs that might be required by the specified generation configuration.
        /// For example, the "stream" output type requires a "streamId" be specified in this parameter.
        /// </summary>
        public Dictionary<string, object> Extra { get; set; } = new Dictionary<string, object>();

        [NonSerialized] private string _userPrefix;
        /// <summary>
        /// A user-specified string to be prepended to the produced artifact names. May be null.
        /// </summary>
        public string UserPrefix
        {
            get => _userPrefix;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    value = UserPrefixReplaceRegex.Replace(value, "");
                    if (value.Length > UserPrefixMaxLength)
                        value = value.Substring(0, UserPrefixMaxLength);
                }

                _userPrefix = value;
            }
        }

        [NonSerialized]
        public Dictionary<Type, IPersistenceConfig> PersistenceConfig;
    }

    public class ConnStringGetter
    {
        public string ConnectionStringSecretName { get; set; }

        private static Regex KVPairRegex = new Regex(@"(\w+)\=(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private IConfiguration _config;
        private Dictionary<string, string> _cachedParsed = null;

        private void UpdateCache()
        {
            // cache the connection string and parse it into a dictionary
            // (from format: "Key1=Value1;Key2=Value2;...;KeyN=ValueN")
            if (ConnectionString == null && ConnectionStringSecretName != null && _config != null)
                _cachedParsed = (ConnectionString = _config.GetSection(ConnectionStringSecretName).Value)
                    .Split(';').ToDictionary(ks => KVPairRegex.Matches(ks)[0].Groups[1].Value, 
                        es => KVPairRegex.Matches(es)[0].Groups[2].Value);
        }

        public void BindConfiguration(IConfiguration config)
        {
            _config = config;
            UpdateCache();
        }

        public string ConnectionString { get; private set; } = null;

        public string AccountName
        {
            get => ConnectionString == null ? null
                    : (_cachedParsed.ContainsKey("AccountName") ? _cachedParsed["AccountName"] : null);
        }

        public string AccountKey
        {
            get => ConnectionString == null ? null 
                    : (_cachedParsed.ContainsKey("AccountKey") ? _cachedParsed["AccountKey"] : null);
        }
    }

    [Serializable]
    public class AzureConfig : IPersistenceConfig
    {
        [Serializable]
        public class KeyVaultConfig
        {
            public string Uri { get; set; }
            public string DefaultKeyName { get; set; }
        }

        [Serializable]
        public class StorageConfig : ConnStringGetter
        {
            public class BlobConfig
            {
                public string ContainerName { get; set; }
                public bool Compress { get; set; } = true;
            }
            
            public BlobConfig Blob { get; set; }
        }

        [Serializable]
        public class DevelopmentConfig
        {
            public bool DisableUpload { get; set; } = false;
            public bool RetainArtifacts { get; set; } = false;  // TODO: wire up!
        }

        public KeyVaultConfig KeyVault { get; set; } = new KeyVaultConfig();
        public StorageConfig Storage { get; set; } = new StorageConfig();
        public DevelopmentConfig DevSettings { get; set; } = new DevelopmentConfig();
    }

    [Serializable]
    public class StreamConfig : ConnStringGetter, IPersistenceConfig { }

    [Serializable]
    public class FilesystemConfig : IPersistenceConfig
    {
        public string PersistDirectory { get; set; } = ".";
    }

    /// <summary>Configurable limits that bound certain runtime behaviors.</summary>
    [Serializable]
    public class LimitsConfig 
    {
        /// <summary>The maximum value of <code>GeneratorConfig.Count</code> permitted.</summary>
        public uint? MaxRecordCountPerJob { get; set; }
        /// <summary>The maximum record count for simple GET-requested jobs.</summary>
        public uint? MaxGETGenerateRecordCountPerJob { get; set; }
        /// <summary>The maximum number of jobs allowed in the queue at once.</summary>
        public uint? MaxQueuedJobs { get; set; }
        /// <summary>The maximum number of records, of any type, allowed in-flight at once.</summary>
        public uint? MaxTotalRecordsInFlight { get; set; }
        /// <summary>The amount of time, in minutes, to keep completed jobs in the queue before removing them.</summary>
        public uint? CompletedJobExpiryTime { get; set; } // in minutes
        /// <summary>
        /// The maximum rate, in Hz, at which any one client can make job-generation requests.
        /// No other request type is bounded by this limit.
        /// </summary>
        public double? MaxJobCreationRequestRate { get; set; } // in Hz
        /// <summary>
        /// The maximum amount of time a simple GET-requested job is allowed to run, in seconds.
        /// <b>Required</b>, defaults to 1 minute ("60") and cannot be set to less than this value.
        /// </summary>
        public uint MaxGETGenerateRunTime { get; set; } = 60; // seconds
    }
}