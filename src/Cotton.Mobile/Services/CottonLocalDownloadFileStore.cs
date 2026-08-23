// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonLocalDownloadFileStore(
        ILogger<CottonLocalDownloadFileStore> logger,
        TimeProvider timeProvider)
    {
        private readonly ILogger<CottonLocalDownloadFileStore> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public Task<bool> DeleteAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string directory = CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, file);
                        if (!Directory.Exists(directory))
                        {
                            return false;
                        }

                        Directory.Delete(directory, recursive: true);
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        CottonLog.WarningWithContext(
                            _logger,
                            "Failed to delete a Cotton mobile local download.",
                            file.Id,
                            exception);
                        throw;
                    }
                },
                cancellationToken);
        }

        public void Commit(
            string temporaryPath,
            string finalPath,
            string directory,
            CottonFileBrowserEntry file)
        {
            StampTemporaryFile(temporaryPath, file.UpdatedAtUtc);
            MoveTemporaryFile(temporaryPath, finalPath);
            Touch(new FileInfo(finalPath));
            DeleteStaleSiblings(directory, finalPath);
        }

        public void DeleteTemporary(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to delete a temporary Cotton mobile download file.",
                    temporaryPath,
                    exception);
            }
        }

        public void Touch(FileInfo info)
        {
            try
            {
                info.LastAccessTimeUtc = _timeProvider.GetUtcNow().UtcDateTime;
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or PlatformNotSupportedException
                    or ArgumentException)
            {
                CottonLog.DebugWithContext(
                    _logger,
                    "Failed to update a Cotton mobile local file timestamp.",
                    info.FullName,
                    exception);
            }
        }

        private void StampTemporaryFile(string path, DateTime updatedAt)
        {
            try
            {
                File.SetLastWriteTimeUtc(path, CottonLocalFileFreshness.NormalizeUtc(updatedAt));
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to stamp a Cotton mobile temporary download file.",
                    path,
                    exception);
                throw;
            }
        }

        private void MoveTemporaryFile(string temporaryPath, string finalPath)
        {
            try
            {
                File.Move(temporaryPath, finalPath, overwrite: true);
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to replace a Cotton mobile download file.",
                    finalPath,
                    exception);
                throw;
            }
        }

        private void DeleteStaleSiblings(string directory, string protectedPath)
        {
            string normalizedProtectedPath = Path.GetFullPath(protectedPath);
            try
            {
                foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (!CottonMobileStoragePaths.IsTemporaryDownloadPath(path)
                        && !string.Equals(Path.GetFullPath(path), normalizedProtectedPath, StringComparison.Ordinal))
                    {
                        DeleteDownload(path);
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CottonLog.DebugWithContext(
                    _logger,
                    "Failed to inspect stale Cotton mobile download files.",
                    directory,
                    exception);
            }
        }

        private void DeleteDownload(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to delete a Cotton mobile download file.",
                    filePath,
                    exception);
            }
        }
    }
}
