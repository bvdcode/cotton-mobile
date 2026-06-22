// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonShareContentStagingStore : ICottonShareContentStagingStore
    {
        private const int MaxSafeFileNameLength = 120;
        private const int MaxSafeFileExtensionLength = 24;
        private const int TruncatedFileNameHashLength = 12;
        private const string DefaultStagedFileName = "shared-content";
        private const string StagingDirectoryName = "Staged";
        private const string TemporaryDirectoryName = ".temp";
        private const string TemporaryFileExtension = ".share";

        private readonly ICottonShareIntakePathProvider _pathProvider;

        public FileSystemCottonShareContentStagingStore(ICottonShareIntakePathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

        public async Task<CottonShareStagedContentSnapshot> StageAsync(
            Guid intakeId,
            Guid itemId,
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (intakeId == Guid.Empty)
            {
                throw new ArgumentException("Share intake id cannot be empty.", nameof(intakeId));
            }

            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Share intake item id cannot be empty.", nameof(itemId));
            }

            string rootDirectory = CreateStagingRootDirectory();
            string itemDirectory = CreateItemDirectory(rootDirectory, intakeId, itemId);
            string temporaryDirectory = CreateTemporaryDirectory(rootDirectory);
            string temporaryPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}{TemporaryFileExtension}");
            string safeFileName = CreateSafeFileName(fileName);
            string stagedPath = Path.Combine(itemDirectory, safeFileName);

            try
            {
                Directory.CreateDirectory(temporaryDirectory);
                await using (var output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                    await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (Directory.Exists(itemDirectory))
                {
                    Directory.Delete(itemDirectory, recursive: true);
                }

                Directory.CreateDirectory(itemDirectory);
                File.Move(temporaryPath, stagedPath, overwrite: true);
                var stagedFile = new FileInfo(stagedPath);
                return new CottonShareStagedContentSnapshot(
                    intakeId,
                    itemId,
                    safeFileName,
                    stagedFile.FullName,
                    stagedFile.Length);
            }
            catch (OperationCanceledException)
            {
                DeleteFile(temporaryPath);
                throw;
            }
            catch
            {
                DeleteFile(temporaryPath);
                throw;
            }
            finally
            {
                TryDeleteDirectoryIfEmpty(temporaryDirectory);
            }
        }

        public Task<IReadOnlyList<CottonShareStagedContentSnapshot>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            string rootDirectory = CreateStagingRootDirectory();
            if (!Directory.Exists(rootDirectory))
            {
                return Task.FromResult<IReadOnlyList<CottonShareStagedContentSnapshot>>([]);
            }

            List<CottonShareStagedContentSnapshot> stagedFiles = [];
            foreach (string intakeDirectory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Guid.TryParseExact(Path.GetFileName(intakeDirectory), "N", out Guid intakeId))
                {
                    continue;
                }

                foreach (string itemDirectory in Directory.EnumerateDirectories(intakeDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Guid.TryParseExact(Path.GetFileName(itemDirectory), "N", out Guid itemId))
                    {
                        continue;
                    }

                    CottonShareStagedContentSnapshot? stagedFile = ResolveStagedFile(intakeId, itemId, itemDirectory);
                    if (stagedFile is not null)
                    {
                        stagedFiles.Add(stagedFile);
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<CottonShareStagedContentSnapshot>>(stagedFiles);
        }

        public Task DeleteIntakeAsync(Guid intakeId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (intakeId == Guid.Empty)
            {
                throw new ArgumentException("Share intake id cannot be empty.", nameof(intakeId));
            }

            DeleteDirectory(Path.Combine(CreateStagingRootDirectory(), intakeId.ToString("N")));
            return Task.CompletedTask;
        }

        public async Task CleanupAsync(
            IReadOnlyCollection<CottonShareIntakeSnapshot> inboxSnapshots,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inboxSnapshots);

            IReadOnlySet<string> referencedKeys = inboxSnapshots
                .SelectMany(snapshot => snapshot.Items.Select(item => new { snapshot.Id, item }))
                .Where(entry => entry.item.HasStagedContent)
                .Select(entry => CreateReferenceKey(entry.Id, entry.item.Id))
                .ToHashSet(StringComparer.Ordinal);
            IReadOnlyList<CottonShareStagedContentSnapshot> stagedFiles =
                await ListAsync(cancellationToken).ConfigureAwait(false);

            foreach (CottonShareStagedContentSnapshot stagedFile in stagedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!referencedKeys.Contains(CreateReferenceKey(stagedFile.IntakeId, stagedFile.ItemId)))
                {
                    DeleteDirectory(CreateItemDirectory(
                        CreateStagingRootDirectory(),
                        stagedFile.IntakeId,
                        stagedFile.ItemId));
                }
            }
        }

        private string CreateStagingRootDirectory()
        {
            return Path.Combine(_pathProvider.CreateShareIntakeDirectory(), StagingDirectoryName);
        }

        private static string CreateItemDirectory(string rootDirectory, Guid intakeId, Guid itemId)
        {
            return Path.Combine(rootDirectory, intakeId.ToString("N"), itemId.ToString("N"));
        }

        private static string CreateTemporaryDirectory(string rootDirectory)
        {
            return Path.Combine(rootDirectory, TemporaryDirectoryName);
        }

        private static string CreateReferenceKey(Guid intakeId, Guid itemId)
        {
            return $"{intakeId:N}/{itemId:N}";
        }

        private static CottonShareStagedContentSnapshot? ResolveStagedFile(
            Guid intakeId,
            Guid itemId,
            string itemDirectory)
        {
            if (!Directory.Exists(itemDirectory))
            {
                return null;
            }

            FileInfo? file = Directory
                .EnumerateFiles(itemDirectory, "*", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(fileInfo => fileInfo.Exists)
                .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
                .ThenBy(fileInfo => fileInfo.FullName, StringComparer.Ordinal)
                .FirstOrDefault();
            return file is null
                ? null
                : new CottonShareStagedContentSnapshot(intakeId, itemId, file.Name, file.FullName, file.Length);
        }

        private static string CreateSafeFileName(string fileName)
        {
            string trimmedName = string.IsNullOrWhiteSpace(fileName) ? DefaultStagedFileName : fileName.Trim();
            string pathSafeName = trimmedName
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            string leafName = Path.GetFileName(pathSafeName);
            if (string.IsNullOrWhiteSpace(leafName))
            {
                leafName = DefaultStagedFileName;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            var buffer = new char[leafName.Length];
            for (int index = 0; index < leafName.Length; index++)
            {
                char character = leafName[index];
                buffer[index] = invalidChars.Contains(character) ? '_' : character;
            }

            string safeName = new string(buffer).Trim();
            if (string.IsNullOrWhiteSpace(safeName) || IsReservedPathSegment(safeName))
            {
                return DefaultStagedFileName;
            }

            return TruncateSafeFileName(safeName);
        }

        private static string TruncateSafeFileName(string safeName)
        {
            if (safeName.Length <= MaxSafeFileNameLength)
            {
                return safeName;
            }

            string extension = Path.GetExtension(safeName);
            if (extension.Length > MaxSafeFileExtensionLength)
            {
                extension = string.Empty;
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension) || IsReservedPathSegment(nameWithoutExtension))
            {
                nameWithoutExtension = DefaultStagedFileName;
            }

            string fullHash = CottonFileUploadHash.CreateSha256Hex(System.Text.Encoding.UTF8.GetBytes(safeName));
            string hash = fullHash[..TruncatedFileNameHashLength];
            int maxNameLength = MaxSafeFileNameLength - extension.Length - hash.Length - 1;
            if (maxNameLength < DefaultStagedFileName.Length)
            {
                maxNameLength = DefaultStagedFileName.Length;
                extension = string.Empty;
            }

            string truncatedName = nameWithoutExtension.Length <= maxNameLength
                ? nameWithoutExtension
                : nameWithoutExtension[..maxNameLength].Trim();
            if (string.IsNullOrWhiteSpace(truncatedName) || IsReservedPathSegment(truncatedName))
            {
                truncatedName = DefaultStagedFileName;
            }

            return $"{truncatedName}-{hash}{extension}";
        }

        private static bool IsReservedPathSegment(string value)
        {
            return string.Equals(value, ".", StringComparison.Ordinal)
                || string.Equals(value, "..", StringComparison.Ordinal);
        }

        private static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }

        private static void TryDeleteDirectoryIfEmpty(string path)
        {
            try
            {
                if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                {
                    Directory.Delete(path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
