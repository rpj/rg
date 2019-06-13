using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Roentgenium.Config;
using Roentgenium.Interfaces;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;

namespace Roentgenium.Stages.Persistence
{
    public class AzurePersistence : PersistenceStageBase
    {
        private readonly string _act;
        private readonly string _key;
        private readonly string _cnt;
        private readonly AzureConfig _config;
        private readonly CloudBlobContainer _blobContainer;

        public override string ToString()
        {
            return $"{base.ToString()}<{_act}, {_cnt}>";
        }

        public AzurePersistence(GeneratorConfig gCfg)
            : base(gCfg)
        {
            if ((bool)!gCfg.PersistenceConfig?.ContainsKey(typeof(AzureConfig)))
                return;

            _config = (AzureConfig)gCfg.PersistenceConfig[typeof(AzureConfig)];

            if (_config.Storage?.AccountKey == null || 
                _config.Storage?.AccountName == null ||
                _config.Storage?.Blob?.ContainerName == null)
                return;

            _act = _config.Storage.AccountName;
            _key = _config.Storage.AccountKey;
            _cnt = _config.Storage.Blob.ContainerName;
            _blobContainer = new CloudStorageAccount(new StorageCredentials(_act, _key), true)
                    .CreateCloudBlobClient().GetContainerReference(_cnt);
            Status = PersistenceStatus.Configured;
            Console.WriteLine($"{this} enabled");
        }

        public override PersistenceStageResult Persist(SinkStageArtifact artifact, Dictionary<string, object> extraMeta = null)
        {
            if (base.Persist(artifact, extraMeta) != null)
            {
                // replace the current artifact with a zipped version for upload
                if (_config.Storage.Blob.Compress)
                {
                    var tempZipName = Path.GetTempFileName();
                    using (var zipFileWriter = new FileStream(tempZipName, FileMode.OpenOrCreate))
                    using (var zipArchive = new ZipArchive(zipFileWriter, ZipArchiveMode.Create))
                    {
                        Status = PersistenceStatus.Compressing;
                        var artifactEntry = zipArchive.CreateEntry(ArtifactName, CompressionLevel.Optimal);
                        using (var zipArtStream = artifactEntry.Open())
                        {
                            artifact.ByteStream.CopyTo(zipArtStream);
                        }
                    }
                    
                    ArtifactName += ".zip";
                    // don't modify the original artifact object, as persisters further down 
                    // the change need it to remain as-emitted from the sink stage
                    artifact = new SinkStageArtifact(this)
                    {
                        Id = artifact.Id,
                        Name = tempZipName,
                        ByteStream = new FileStream(tempZipName, FileMode.Open, FileAccess.Read, FileShare.Read)
                    };
                }

                var fSize = new FileInfo(artifact.Name).Length;

                // this matches the format (B64-encoded MD5) that Azure Blobs natively checksum with
                var md5Base64 = Convert.ToBase64String(
                    new MD5CryptoServiceProvider().ComputeHash(artifact.ByteStream));
                artifact.ByteStream.Seek(0, SeekOrigin.Begin);

                var uploadSw = new Stopwatch();
                var blobUrl = $"https://{_act}.blob.core.windows.net/{_cnt}/{ArtifactName}";

                if (_config.DevSettings != null && _config.DevSettings.DisableUpload)
                    Console.WriteLine("DevSettings.DisableUpload=True, skipping upload.");
                else
                {
                    CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(ArtifactName);
                    Console.WriteLine($"{this} uploading:\n\tPipelineId: {_genConfig.Id}" +
                        $"\n\tSize: {fSize}\n\tBlob: {blobUrl}");

                    Status = PersistenceStatus.Uploading;
                    uploadSw.Start();
                    blob.UploadFromStream(artifact.ByteStream);

                    blob.Metadata["specification"] = _genConfig.Specification.ToString();
                    blob.Metadata["recordcount"] = _genConfig.Count.ToString();
                    blob.Metadata["sizebytes"] = fSize.ToString();
                    blob.Metadata["pipelineid"] = _genConfig.Id.ToString();
                    blob.Metadata["artifactid"] = artifact.Id.ToString();
                    blob.Metadata["contentmd5"] = md5Base64;
                    blob.Metadata["timestamp"] = Timestamp.ToString("O");

                    if (!string.IsNullOrEmpty(_genConfig.UserPrefix))
                        blob.Metadata["userprefix"] = _genConfig.UserPrefix;

                    if (extraMeta != null)
                    {
                        if (extraMeta.ContainsKey("pipelineInitiator") && extraMeta["pipelineInitiator"] != null)
                            blob.Metadata["remote"] = ((PipelineRequestTracker)extraMeta["pipelineInitiator"]).RemoteAddr;

                        if (extraMeta.ContainsKey("tranformStageElapsed"))
                            blob.Metadata["elapsed"] = extraMeta["tranformStageElapsed"].ToString();
                    }

                    if (_genConfig.Filters.Count > 0)
                        blob.Metadata["defectlist"] = string.Join(',', _genConfig.Filters);

                    blob.SetMetadata();

                    uploadSw.Stop();
                    Console.WriteLine($"{this}: {artifact.Id} uploaded in {uploadSw.Elapsed}");
                    Status = PersistenceStatus.CleaningUp;
                }

                // cleanup the extra zip temp file, if extant
                if (_config.Storage.Blob.Compress)
                    artifact.Cleanup();

                Status = PersistenceStatus.Success;
                return new PersistenceStageResult()
                {
                    Id = Id,
                    PipelineId = _genConfig.Id,
                    Timestamp = Timestamp,
                    Success = true,
                    Meta = new Dictionary<string, object>()
                    {
                        { "container", _cnt },
                        { "blob", new Dictionary<string, object>()
                            {
                                { "id", artifact.Id.ToString() },
                                { "specification", _genConfig.Specification.ToString().ToLower() },
                                { "recordCount", _genConfig.Count.ToString() },
                                { "sizeBytes", fSize },
                                { "contentMD5", md5Base64 },
                                { "uploadDuration", uploadSw.Elapsed },
                                { "url", blobUrl }
                            }
                        }
                    }
                };
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
