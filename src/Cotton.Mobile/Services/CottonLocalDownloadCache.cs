// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonLocalDownloadCache(
        ILogger<CottonLocalDownloadCache> logger,
        TimeProvider timeProvider) : ICottonLocalDownloadCache
    {
        private readonly ILogger<CottonLocalDownloadCache> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public CottonLocalFileSnapshot? GetLocalDownload(Uri instanceUri, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            return Inspect(
                file,
                "local download snapshot",
                () =>
                {
                    FileInfo? info = GetLocalDownloadFile(instanceUri, file);
                    return info is null ? null : CreateLocalFileSnapshot(info);
                });
        }

        public CottonLocalFileSnapshot? GetReusableLocalDownloadSnapshot(
            Uri instanceUri,
            CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            return Inspect(
                file,
                "reusable local download snapshot",
                () =>
                {
                    FileInfo? info = GetReusableLocalDownloadFile(instanceUri, file);
                    return info is null ? null : CreateLocalFileSnapshot(info);
                });
        }

        public CottonFileDownloadResult? GetReusableLocalDownload(
            Uri instanceUri,
            CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            return Inspect(
                file,
                "reusable local download",
                () =>
                {
                    FileInfo? info = GetReusableLocalDownloadFile(instanceUri, file);
                    if (info is null)
                    {
                        return null;
                    }

                    Touch(info);
                    return new CottonFileDownloadResult(file.Name, info.FullName, info.Length, file.ContentType);
                });
        }

        public Task<bool> DeleteLocalDownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string directory = CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, file);
                    if (!Directory.Exists(directory))
                    {
                        return false;
                    }

                    Directory.Delete(directory, recursive: true);
                    return true;
                },
                cancellationToken);
        }

        public void CommitDownload(
            string temporaryPath,
            string finalPath,
            string directory,
            CottonFileBrowserEntry file)
        {
            try
            {
                File.SetLastWriteTimeUtc(
                    temporaryPath,
                    CottonLocalFileFreshness.NormalizeUtc(file.UpdatedAtUtc));
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to stamp a Cotton mobile temporary download file.",
                    temporaryPath,
                    exception);
                throw;
            }

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

            Touch(new FileInfo(finalPath));
            DeleteStaleSiblingDownloads(directory, finalPath);
        }

        public void DeleteTemporaryDownload(string temporaryPath)
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

        private static FileInfo? GetLocalDownloadFile(Uri instanceUri, CottonFileBrowserEntry file)
        {
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                return null;
            }

            FileInfo info = new(CottonMobileStoragePaths.CreateDownloadPath(instanceUri, file));
            return info.Exists ? info : null;
        }

        private static FileInfo? GetReusableLocalDownloadFile(Uri instanceUri, CottonFileBrowserEntry file)
        {
            if (!CottonSensitiveFileCachePolicy.CanReuseUnpinnedLocalCopy(file))
            {
                return null;
            }

            FileInfo? info = GetLocalDownloadFile(instanceUri, file);
            if (info is null || !IsReusable(file, info))
            {
                return null;
            }

            return info;
        }

        private static CottonLocalFileSnapshot CreateLocalFileSnapshot(FileInfo info)
        {
            return new CottonLocalFileSnapshot(
                info.Name,
                info.Length,
                CottonLocalFileFreshness.NormalizeUtc(info.LastWriteTimeUtc));
        }

        private static bool IsReusable(CottonFileBrowserEntry file, FileInfo info)
        {
            if ((file.SizeBytes.HasValue && file.SizeBytes.Value != info.Length)
                || !CottonLocalFileFreshness.IsFresh(info.LastWriteTimeUtc, file.UpdatedAtUtc)
                || file.ContentHash is null)
            {
                return false;
            }

            using FileStream content = info.OpenRead();
            string contentHash = CottonContentHash.ComputeSha256(content);
            return string.Equals(contentHash, file.ContentHash, StringComparison.Ordinal);
        }

        private T? Inspect<T>(CottonFileBrowserEntry file, string operation, Func<T?> inspect)
            where T : class
        {
            try
            {
                return inspect();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CottonLog.DebugWithFileId(
                    _logger,
                    "Failed to inspect a Cotton mobile download.",
                    operation,
                    file.Id,
                    exception);
                return null;
            }
        }

        private void Touch(FileInfo info)
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

        private void DeleteStaleSiblingDownloads(string directory, string protectedPath)
        {
            string normalizedProtectedPath = Path.GetFullPath(protectedPath);
            try
            {
                foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (CottonMobileStoragePaths.IsTemporaryDownloadPath(path)
                        || string.Equals(Path.GetFullPath(path), normalizedProtectedPath, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    DeleteDownload(path);
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
