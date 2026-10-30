// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public static class CottonAtomicJsonFile
    {
        private const int BufferSize = 16384;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions =
            new(JsonSerializerDefaults.Web);

        public static async Task<T?> ReadAsync<T>(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            await using FileStream stream = new(
                filePath,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.Read,
                    BufferSize = BufferSize,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                });
            return await JsonSerializer
                .DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        public static async Task WriteAsync<T>(
            string filePath,
            T value,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(value);

            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A parent directory is required.", nameof(filePath));
            }

            string temporaryFilePath = CreateTemporaryFilePath(filePath);
            try
            {
                Directory.CreateDirectory(directory);
                await using (FileStream stream = new(
                    temporaryFilePath,
                    new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.Write,
                        Share = FileShare.None,
                        BufferSize = BufferSize,
                        Options = FileOptions.Asynchronous | FileOptions.WriteThrough,
                    }))
                {
                    await JsonSerializer
                        .SerializeAsync(stream, value, SerializerOptions, cancellationToken)
                        .ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryFilePath, filePath, overwrite: true);
            }
            catch (Exception exception)
                when (exception is OperationCanceledException
                    or IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or NotSupportedException)
            {
                DeleteTemporaryFile(temporaryFilePath, exception);
                throw;
            }
        }

        public static void DeleteIfExists(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static string CreateTemporaryFilePath(string filePath)
        {
            return $"{filePath}.{Guid.NewGuid():N}{TemporaryFileExtension}";
        }

        private static void DeleteTemporaryFile(string temporaryFilePath, Exception originalException)
        {
            try
            {
                DeleteIfExists(temporaryFilePath);
            }
            catch (Exception cleanupException)
                when (cleanupException is IOException or UnauthorizedAccessException)
            {
                throw new AggregateException(originalException, cleanupException);
            }
        }
    }
}
