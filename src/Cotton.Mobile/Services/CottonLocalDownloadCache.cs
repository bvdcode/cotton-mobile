// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonLocalDownloadCache(
        CottonLocalDownloadFileStore fileStore,
        ILogger<CottonLocalDownloadCache> logger) : ICottonLocalDownloadCache
    {
        private readonly CottonLocalDownloadFileStore _fileStore =
            fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        private readonly ILogger<CottonLocalDownloadCache> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

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

                    _fileStore.Touch(info);
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

            return _fileStore.DeleteAsync(instanceUri, file, cancellationToken);
        }

        public void CommitDownload(
            string temporaryPath,
            string finalPath,
            string directory,
            CottonFileBrowserEntry file)
        {
            _fileStore.Commit(temporaryPath, finalPath, directory, file);
        }

        public void DeleteTemporaryDownload(string temporaryPath)
        {
            _fileStore.DeleteTemporary(temporaryPath);
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

    }
}
