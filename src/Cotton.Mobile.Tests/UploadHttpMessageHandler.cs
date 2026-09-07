using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Cotton.Files;
using Cotton.Settings;

namespace Cotton.Mobile.Tests
{
    internal class UploadHttpMessageHandler : HttpMessageHandler
    {
        public const int ChunkSize = 4 * 1024 * 1024;

        public Dictionary<string, byte[]> Chunks { get; } = new(StringComparer.Ordinal);

        public List<string> UploadedHashes { get; } = [];

        public List<CreateFileFromChunksRequestDto> PublishedFiles { get; } = [];

        public int? FailOnUploadNumber { get; init; }

        public HttpStatusCode FailureStatus { get; init; } = HttpStatusCode.ServiceUnavailable;

        public CancellationTokenSource? CancelAfterChunk { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Uri uri = request.RequestUri ?? throw new InvalidOperationException("Request URI is missing.");
            switch (uri.AbsolutePath)
            {
                case "/api/v1/settings":
                    return Json(new ClientSettingsDto
                    {
                        MaxChunkSizeBytes = ChunkSize,
                        SupportedHashAlgorithm = "SHA256",
                    });
                case "/api/v1/chunks/raw":
                    return await ReceiveChunkAsync(request, cancellationToken);
                case "/api/v1/files/from-chunks":
                    return await PublishFileAsync(request, cancellationToken);
                default:
                    if (uri.AbsolutePath.StartsWith("/api/v1/chunks/", StringComparison.Ordinal)
                        && uri.AbsolutePath.EndsWith("/exists", StringComparison.Ordinal))
                    {
                        string hash = uri.Segments[^2].TrimEnd('/');
                        return Json(Chunks.ContainsKey(hash));
                    }

                    throw new InvalidOperationException($"Unexpected upload request: {request.Method} {uri.AbsolutePath}");
            }
        }

        private async Task<HttpResponseMessage> ReceiveChunkAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpContent content = request.Content ?? throw new InvalidOperationException("Chunk body is missing.");
            byte[] bytes = await content.ReadAsByteArrayAsync(cancellationToken);
            string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (request.RequestUri?.Query != "?hash=" + hash)
            {
                throw new InvalidDataException("Uploaded bytes do not match the requested chunk hash.");
            }

            UploadedHashes.Add(hash);
            if (UploadedHashes.Count == FailOnUploadNumber)
            {
                return new HttpResponseMessage(FailureStatus)
                {
                    Content = new StringContent("Upload rejected."),
                };
            }

            Chunks.Add(hash, bytes);
            if (CancelAfterChunk is not null)
            {
                await CancelAfterChunk.CancelAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private async Task<HttpResponseMessage> PublishFileAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpContent content = request.Content ?? throw new InvalidOperationException("File body is missing.");
            CreateFileFromChunksRequestDto file = await content
                .ReadFromJsonAsync<CreateFileFromChunksRequestDto>(cancellationToken)
                ?? throw new InvalidDataException("File request is missing.");
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long length = 0;
            foreach (string chunk in file.ChunkHashes)
            {
                byte[] bytes = Chunks[chunk];
                hash.AppendData(bytes);
                length += bytes.Length;
            }

            if (file.Hash != Convert.ToHexStringLower(hash.GetHashAndReset()))
            {
                throw new InvalidDataException("Published content hash does not match uploaded chunks.");
            }

            PublishedFiles.Add(file);
            return Json(new NodeFileManifestDto
            {
                Id = Guid.NewGuid(),
                Name = file.Name,
                SizeBytes = length,
                ContentType = file.ContentType,
                ContentHash = file.Hash,
                Metadata = file.Metadata ?? throw new InvalidDataException("Upload metadata is missing."),
            });
        }

        private static HttpResponseMessage Json<T>(T value)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(value, options: JsonSerializerOptions.Web),
            };
        }
    }
}
