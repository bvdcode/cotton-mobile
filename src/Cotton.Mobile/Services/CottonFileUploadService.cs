using System.Security.Cryptography;
using Cotton.Files;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonFileUploadService : ICottonFileUploadService
    {
        private readonly ICottonClientFactory _clientFactory;

        public CottonFileUploadService(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
        }

        public async Task<CottonFileBrowserEntry> UploadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CottonFileUploadSource source,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(folder);
            ArgumentNullException.ThrowIfNull(source);

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            CottonFileUploadResult result = await UploadContentAsync(
                client,
                source,
                progress,
                cancellationToken).ConfigureAwait(false);

            CreateFileFromChunksRequestDto request = CreateFileFromChunksRequest(
                folder,
                source,
                result);
            NodeFileManifestDto createdFile = await client.Files.CreateFromChunksAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return CottonFileBrowserEntry.FromFile(createdFile);
        }

        public async Task<CottonFileBrowserEntry> UpdateContentAsync(
            Uri instanceUri,
            Guid fileId,
            CottonFolderHandle folder,
            string expectedETag,
            CottonFileUploadSource source,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(folder);
            ArgumentNullException.ThrowIfNull(source);
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id cannot be empty.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(expectedETag))
            {
                throw new ArgumentException("Expected file ETag is required.", nameof(expectedETag));
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            CottonFileUploadResult result = await UploadContentAsync(
                client,
                source,
                progress,
                cancellationToken).ConfigureAwait(false);

            CreateFileFromChunksRequestDto request = CreateFileFromChunksRequest(
                folder,
                source,
                result,
                fileId);
            NodeFileManifestDto updatedFile = await client.Files
                .UpdateContentAsync(fileId, request, expectedETag.Trim(), cancellationToken)
                .ConfigureAwait(false);
            return CottonFileBrowserEntry.FromFile(updatedFile);
        }

        private static async Task<CottonFileUploadResult> UploadContentAsync(
            ICottonCloudClient client,
            CottonFileUploadSource source,
            IProgress<long>? progress,
            CancellationToken cancellationToken)
        {
            var serverSettings = await client.Settings.GetAsync(cancellationToken).ConfigureAwait(false);
            var uploadSettings = new CottonFileUploadSettings(
                serverSettings.MaxChunkSizeBytes,
                serverSettings.SupportedHashAlgorithm);

            await using Stream content = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            CottonFileUploadResult result = await UploadChunksAsync(
                client,
                content,
                uploadSettings,
                progress,
                cancellationToken).ConfigureAwait(false);
            return result;
        }

        private static CreateFileFromChunksRequestDto CreateFileFromChunksRequest(
            CottonFolderHandle folder,
            CottonFileUploadSource source,
            CottonFileUploadResult result,
            Guid? originalNodeFileId = null)
        {
            return new CreateFileFromChunksRequestDto
            {
                NodeId = folder.Id,
                ChunkHashes = result.ChunkHashes,
                Name = source.Snapshot.Name,
                ContentType = source.Snapshot.ContentType,
                Hash = result.ContentHash,
                OriginalNodeFileId = originalNodeFileId,
                Metadata = new Dictionary<string, string>(source.Snapshot.Metadata, StringComparer.Ordinal),
                Validate = true,
            };
        }

        private static async Task<CottonFileUploadResult> UploadChunksAsync(
            ICottonCloudClient client,
            Stream content,
            CottonFileUploadSettings settings,
            IProgress<long>? progress,
            CancellationToken cancellationToken)
        {
            var chunkHashes = new List<string>();
            byte[] buffer = new byte[settings.MaxChunkSizeBytes];
            long uploadedBytes = 0;

            using var contentHash = SHA256.Create();
            while (true)
            {
                int bytesRead = await ReadChunkAsync(content, buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                contentHash.TransformBlock(buffer, 0, bytesRead, null, 0);
                string chunkHash = CottonFileUploadHash.CreateSha256Hex(buffer.AsSpan(0, bytesRead));
                chunkHashes.Add(chunkHash);

                if (!await client.Chunks.ExistsAsync(chunkHash, cancellationToken).ConfigureAwait(false))
                {
                    using var chunkStream = new MemoryStream(buffer, 0, bytesRead, writable: false);
                    await client.Chunks.UploadRawAsync(
                            chunkHash,
                            chunkStream,
                            CottonFileUploadSourceSnapshot.DefaultContentType,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                uploadedBytes += bytesRead;
                progress?.Report(uploadedBytes);
            }

            contentHash.TransformFinalBlock([], 0, 0);
            byte[] fileHash = contentHash.Hash
                ?? throw new InvalidOperationException("Upload content hash was not finalized.");
            return new CottonFileUploadResult(
                chunkHashes,
                CottonFileUploadHash.FormatHex(fileHash));
        }

        private static async Task<int> ReadChunkAsync(
            Stream content,
            byte[] buffer,
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < buffer.Length)
            {
                int bytesRead = await content.ReadAsync(
                        buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
