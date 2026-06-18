using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Cotton.Mobile.Services
{
    public class FileThumbnailCache : IFileThumbnailCache
    {
        private const string CacheFileExtension = ".webp";
        private const string CacheTempFileSearchPattern = "*.webp.*.tmp";
        private const string WebPContentType = "image/webp";

        private readonly HttpClient _httpClient;
        private readonly FileThumbnailCacheOptions _options;
        private readonly ILogger<FileThumbnailCache> _logger;
        private readonly ConcurrentDictionary<string, Lazy<Task>> _warmups = new(StringComparer.Ordinal);
        private readonly SemaphoreSlim _downloadGate;

        public FileThumbnailCache(
            HttpClient httpClient,
            FileThumbnailCacheOptions options,
            ILogger<FileThumbnailCache> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClient = httpClient;
            _options = options;
            _logger = logger;
            _downloadGate = new SemaphoreSlim(options.MaxParallelDownloads, options.MaxParallelDownloads);
        }

        public string? GetCachedThumbnailSource(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Thumbnail cache key is required.", nameof(cacheKey));
            }

            string path = CreateCachePath(cacheKey);
            if (!TryGetReadyCacheFile(path, cacheKey, out FileInfo? file) || file is null)
            {
                return null;
            }

            try
            {
                file.LastWriteTimeUtc = DateTime.UtcNow;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Could not update thumbnail cache access time for {CacheKey}.", cacheKey);
            }

            return path;
        }

        public Task WarmAsync(
            string cacheKey,
            Uri sourceUri,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Thumbnail cache key is required.", nameof(cacheKey));
            }

            ArgumentNullException.ThrowIfNull(sourceUri);
            cancellationToken.ThrowIfCancellationRequested();

            string normalizedCacheKey = cacheKey.Trim();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Lazy<Task> warmup = _warmups.GetOrAdd(
                    normalizedCacheKey,
                    _ => CreateWarmup(normalizedCacheKey, sourceUri, cancellationToken));
                Task warmupTask = warmup.Value;
                if (!warmupTask.IsCanceled)
                {
                    return warmupTask;
                }

                TryRemoveWarmup(normalizedCacheKey, warmup);
            }
        }

        private Lazy<Task> CreateWarmup(
            string cacheKey,
            Uri sourceUri,
            CancellationToken cancellationToken)
        {
            Lazy<Task>? warmup = null;
            warmup = new Lazy<Task>(
                () => WarmAndRemoveAsync(cacheKey, warmup!, sourceUri, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication);
            return warmup;
        }

        private async Task WarmAndRemoveAsync(
            string cacheKey,
            Lazy<Task> warmup,
            Uri sourceUri,
            CancellationToken cancellationToken)
        {
            try
            {
                await WarmCoreAsync(cacheKey, sourceUri, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                TryRemoveWarmup(cacheKey, warmup);
            }
        }

        private bool TryRemoveWarmup(string cacheKey, Lazy<Task> warmup)
        {
            return ((ICollection<KeyValuePair<string, Lazy<Task>>>)_warmups)
                .Remove(new KeyValuePair<string, Lazy<Task>>(cacheKey, warmup));
        }

        private async Task WarmCoreAsync(
            string cacheKey,
            Uri sourceUri,
            CancellationToken cancellationToken)
        {
            string targetPath = CreateCachePath(cacheKey);
            if (TryGetReadyCacheFile(targetPath, cacheKey, out _))
            {
                return;
            }

            await _downloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (TryGetReadyCacheFile(targetPath, cacheKey, out _))
                {
                    return;
                }

                Directory.CreateDirectory(CacheDirectory);
                using HttpResponseMessage response = await _httpClient.GetAsync(
                    sourceUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "Thumbnail cache warm-up skipped {SourceUri}: HTTP {StatusCode}.",
                        sourceUri,
                        response.StatusCode);
                    return;
                }

                if (!IsSupportedThumbnailContentType(response))
                {
                    _logger.LogDebug(
                        "Thumbnail cache warm-up skipped {SourceUri}: unsupported content type {ContentType}.",
                        sourceUri,
                        response.Content.Headers.ContentType?.ToString() ?? "unknown");
                    return;
                }

                long? contentLength = response.Content.Headers.ContentLength;
                if (contentLength > _options.MaxEntryBytes)
                {
                    _logger.LogDebug(
                        "Thumbnail cache warm-up skipped {SourceUri}: {ContentLength} bytes exceeds {MaxEntryBytes}.",
                        sourceUri,
                        contentLength,
                        _options.MaxEntryBytes);
                    return;
                }

                string tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
                try
                {
                    await using Stream source = await response.Content
                        .ReadAsStreamAsync(cancellationToken)
                        .ConfigureAwait(false);
                    await using var destination = new FileStream(
                        tempPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 32768,
                        useAsync: true);
                    await CopyWithLimitAsync(source, destination, _options.MaxEntryBytes, cancellationToken)
                        .ConfigureAwait(false);
                    if (destination.Length == 0)
                    {
                        throw new IOException("Thumbnail cache entry was empty.");
                    }

                    File.Move(tempPath, targetPath, overwrite: true);
                }
                finally
                {
                    DeleteTempFile(tempPath);
                }

                await CleanupAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Thumbnail cache warm-up failed for {SourceUri}.", sourceUri);
            }
            finally
            {
                _downloadGate.Release();
            }
        }

        private async Task CleanupAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(CacheDirectory))
            {
                return;
            }

            DeleteAbandonedTempFiles(cancellationToken);
            List<FileInfo> files = Directory
                .EnumerateFiles(CacheDirectory, "*" + CacheFileExtension, SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(file => file.Exists)
                .OrderBy(file => file.LastWriteTimeUtc)
                .ToList();

            long totalBytes = files.Sum(file => file.Length);
            foreach (FileInfo file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (totalBytes <= _options.MaxCacheBytes)
                {
                    return;
                }

                try
                {
                    long length = file.Length;
                    file.Delete();
                    totalBytes -= length;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    _logger.LogDebug(exception, "Could not delete thumbnail cache file {Path}.", file.FullName);
                }
            }
        }

        private void DeleteAbandonedTempFiles(CancellationToken cancellationToken)
        {
            DateTime utcNow = DateTime.UtcNow;
            foreach (string path in Directory.EnumerateFiles(
                CacheDirectory,
                CacheTempFileSearchPattern,
                SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = new FileInfo(path);
                if (!file.Exists || !CottonTemporaryFilePolicy.IsAbandoned(file, utcNow))
                {
                    continue;
                }

                DeleteTempFile(path);
            }
        }

        private string CacheDirectory => CottonMobileStoragePaths.CreateThumbnailCacheDirectory(_options);

        private string CreateCachePath(string cacheKey)
        {
            string fileName = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cacheKey)))
                .ToLowerInvariant();
            return Path.Combine(CacheDirectory, fileName + CacheFileExtension);
        }

        private static bool IsSupportedThumbnailContentType(HttpResponseMessage response)
        {
            string? mediaType = response.Content.Headers.ContentType?.MediaType;
            return string.Equals(mediaType, WebPContentType, StringComparison.OrdinalIgnoreCase);
        }

        private bool TryGetReadyCacheFile(string path, string cacheKey, out FileInfo? file)
        {
            file = null;

            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    return false;
                }

                if (info.Length > 0)
                {
                    file = info;
                    return true;
                }

                DeleteEmptyCacheFile(path, cacheKey);
                return false;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Could not inspect thumbnail cache file for {CacheKey}.", cacheKey);
                return false;
            }
        }

        private void DeleteEmptyCacheFile(string path, string cacheKey)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Could not delete empty thumbnail cache file for {CacheKey}.", cacheKey);
            }
        }

        private static async Task CopyWithLimitAsync(
            Stream source,
            Stream destination,
            long maxBytes,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[32768];
            long totalBytes = 0;
            while (true)
            {
                int read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                totalBytes += read;
                if (totalBytes > maxBytes)
                {
                    throw new IOException("Thumbnail cache entry exceeded the configured size limit.");
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private void DeleteTempFile(string tempPath)
        {
            if (!File.Exists(tempPath))
            {
                return;
            }

            try
            {
                File.Delete(tempPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Could not delete temporary thumbnail cache file {Path}.", tempPath);
            }
        }
    }
}
