using System.Net;
using Cotton.Files;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileUploadTransportTests
    {
        private static readonly Uri Instance = new("https://cotton.test");
        private static readonly CottonFolderHandle Destination = new(Guid.NewGuid(), "Uploads");

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(UploadHttpMessageHandler.ChunkSize * 2 + 123)]
        public async Task UploadPublishesExactContentAndOperationMetadata(int size)
        {
            using UploadHttpMessageHandler handler = new();
            using HttpClient httpClient = new(handler);
            CottonFileUploadService service = new(new UploadTestClientFactory(httpClient));
            byte[] bytes = CreateContent(size);
            CottonFileUploadSource source = CreateSource(bytes);

            CottonFileBrowserEntry result = await service.UploadAsync(
                Instance, Destination, source, cancellationToken: TestContext.Current.CancellationToken);

            CreateFileFromChunksRequestDto published = Assert.Single(handler.PublishedFiles);
            Assert.Equal(Destination.Id, published.NodeId);
            Assert.True(published.Validate);
            Assert.NotNull(published.Metadata);
            Assert.Equal("operation-1", published.Metadata[CottonFileUploadMetadataKeys.UploadOperationId]);
            Assert.Equal("video.mp4", result.Name);
            Assert.Equal(bytes.LongLength, result.SizeBytes);
            Assert.Equal(CottonContentHash.ComputeSha256(bytes), result.ContentHash);
            Assert.All(handler.Chunks.Values, chunk => Assert.InRange(chunk.Length, 1, UploadHttpMessageHandler.ChunkSize));
        }

        [Theory]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.InsufficientStorage)]
        public async Task RetryReusesAcceptedChunksAndPublishesOnlyAfterCompletion(HttpStatusCode status)
        {
            using UploadHttpMessageHandler handler = new() { FailOnUploadNumber = 2, FailureStatus = status };
            using HttpClient httpClient = new(handler);
            CottonFileUploadService service = new(new UploadTestClientFactory(httpClient));
            byte[] bytes = CreateContent(UploadHttpMessageHandler.ChunkSize * 2 + 123);
            CottonFileUploadSource source = CreateSource(bytes);

            CottonApiException failure = await Assert.ThrowsAsync<CottonApiException>(() => service.UploadAsync(
                Instance, Destination, source, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Equal(status, failure.StatusCode);
            Assert.Empty(handler.PublishedFiles);
            string acceptedHash = Assert.Single(handler.Chunks).Key;

            CottonFileBrowserEntry result = await service.UploadAsync(
                Instance, Destination, source, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Single(handler.PublishedFiles);
            Assert.Equal(1, handler.UploadedHashes.Count(hash => hash == acceptedHash));
            Assert.Equal(3, handler.Chunks.Count);
            Assert.Equal(CottonContentHash.ComputeSha256(bytes), result.ContentHash);
        }

        [Fact]
        public async Task ChangedSourceIsNotPublished()
        {
            using UploadHttpMessageHandler handler = new();
            using HttpClient httpClient = new(handler);
            CottonFileUploadService service = new(new UploadTestClientFactory(httpClient));
            byte[] bytes = CreateContent(100);
            CottonFileUploadSource source = CreateSource(bytes);
            bytes[0] ^= 1;

            await Assert.ThrowsAsync<InvalidDataException>(() => service.UploadAsync(
                Instance, Destination, source, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Empty(handler.PublishedFiles);
        }

        [Fact]
        public async Task CancelledUploadDoesNotPublishIncompleteFile()
        {
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
            using UploadHttpMessageHandler handler = new() { CancelAfterChunk = cancellation };
            using HttpClient httpClient = new(handler);
            CottonFileUploadService service = new(new UploadTestClientFactory(httpClient));
            CottonFileUploadSource source = CreateSource(CreateContent(UploadHttpMessageHandler.ChunkSize + 123));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UploadAsync(
                Instance, Destination, source, cancellationToken: cancellation.Token));

            Assert.Single(handler.Chunks);
            Assert.Empty(handler.PublishedFiles);
        }

        private static byte[] CreateContent(int size)
        {
            byte[] bytes = new byte[size];
            new Random(42).NextBytes(bytes);
            return bytes;
        }

        private static CottonFileUploadSource CreateSource(byte[] bytes)
        {
            return new CottonFileUploadSource(
                new CottonFileUploadSourceSnapshot(
                    "video.mp4", "video/mp4", bytes.LongLength,
                    new Dictionary<string, string> { [CottonFileUploadMetadataKeys.UploadOperationId] = "operation-1" },
                    CottonContentHash.ComputeSha256(bytes)),
                cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
                });
        }
    }
}
