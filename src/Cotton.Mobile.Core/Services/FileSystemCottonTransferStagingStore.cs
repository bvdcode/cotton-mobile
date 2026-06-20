namespace Cotton.Mobile.Services
{
    public class FileSystemCottonTransferStagingStore : ICottonTransferStagingStore
    {
        private const int MaxSafeFileNameLength = 120;
        private const int MaxSafeFileExtensionLength = 24;
        private const int TruncatedFileNameHashLength = 12;
        private const string DefaultStagedFileName = "upload";
        private const string TemporaryDirectoryName = ".temp";
        private const string TemporaryFileExtension = ".stage";

        private readonly ICottonTransferStagingPathProvider _pathProvider;

        public FileSystemCottonTransferStagingStore(ICottonTransferStagingPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

        public async Task<CottonTransferStagedFileSnapshot> StageAsync(
            Uri instanceUri,
            Guid transferId,
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);
            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            string safeFileName = CreateSafeFileName(fileName);
            string rootDirectory = _pathProvider.CreateTransferStagingDirectory(instanceUri);
            string transferDirectory = CreateTransferDirectory(rootDirectory, transferId);
            string temporaryDirectory = CreateTemporaryDirectory(rootDirectory);
            string temporaryPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}{TemporaryFileExtension}");
            string stagedPath = Path.Combine(transferDirectory, safeFileName);

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
                if (Directory.Exists(transferDirectory))
                {
                    Directory.Delete(transferDirectory, recursive: true);
                }

                Directory.CreateDirectory(transferDirectory);
                File.Move(temporaryPath, stagedPath, overwrite: true);
                var file = new FileInfo(stagedPath);
                return new CottonTransferStagedFileSnapshot(transferId, safeFileName, stagedPath, file.Length);
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

        public Task<CottonTransferStagedFileSnapshot?> GetAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();
            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            string transferDirectory = CreateTransferDirectory(
                _pathProvider.CreateTransferStagingDirectory(instanceUri),
                transferId);
            return Task.FromResult(ResolveStagedFile(transferId, transferDirectory));
        }

        public Task<IReadOnlyList<CottonTransferStagedFileSnapshot>> ListAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            string rootDirectory = _pathProvider.CreateTransferStagingDirectory(instanceUri);
            if (!Directory.Exists(rootDirectory))
            {
                return Task.FromResult<IReadOnlyList<CottonTransferStagedFileSnapshot>>([]);
            }

            List<CottonTransferStagedFileSnapshot> stagedFiles = [];
            foreach (string directory in Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out Guid transferId))
                {
                    continue;
                }

                CottonTransferStagedFileSnapshot? stagedFile = ResolveStagedFile(transferId, directory);
                if (stagedFile is not null)
                {
                    stagedFiles.Add(stagedFile);
                }
            }

            return Task.FromResult<IReadOnlyList<CottonTransferStagedFileSnapshot>>(stagedFiles);
        }

        public Task DeleteAsync(Uri instanceUri, Guid transferId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();
            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            DeleteDirectory(CreateTransferDirectory(_pathProvider.CreateTransferStagingDirectory(instanceUri), transferId));
            return Task.CompletedTask;
        }

        public async Task<CottonTransferStagedFileCleanupResult> CleanupAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonTransferQueueItem> queueItems,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queueItems);

            IReadOnlyList<CottonTransferStagedFileSnapshot> stagedFiles =
                await ListAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            Dictionary<Guid, CottonTransferStagedFileSnapshot> stagedFilesByTransferId =
                stagedFiles.ToDictionary(file => file.TransferId);
            IReadOnlySet<Guid> transferIdsToDelete =
                CottonTransferStagedFileCleanupPolicy.ResolveTransferIdsToDelete(
                    queueItems,
                    stagedFilesByTransferId.Keys.ToList());
            foreach (Guid transferId in transferIdsToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await DeleteAsync(instanceUri, transferId, cancellationToken).ConfigureAwait(false);
            }

            IReadOnlyList<CottonTransferStagedFileSnapshot> remainingStagedFiles =
                await ListAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            HashSet<Guid> remainingTransferIds = remainingStagedFiles
                .Select(file => file.TransferId)
                .ToHashSet();
            IReadOnlyList<CottonTransferStagedFileSnapshot> deletedFiles = transferIdsToDelete
                .Where(transferId => !remainingTransferIds.Contains(transferId))
                .Select(transferId => stagedFilesByTransferId[transferId])
                .ToList();
            return new CottonTransferStagedFileCleanupResult(
                deletedFiles.Count,
                deletedFiles.Sum(file => file.SizeBytes));
        }

        private static CottonTransferStagedFileSnapshot? ResolveStagedFile(Guid transferId, string transferDirectory)
        {
            if (!Directory.Exists(transferDirectory))
            {
                return null;
            }

            FileInfo? file = Directory
                .EnumerateFiles(transferDirectory, "*", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .Where(fileInfo => fileInfo.Exists)
                .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
                .ThenBy(fileInfo => fileInfo.FullName, StringComparer.Ordinal)
                .FirstOrDefault();
            return file is null
                ? null
                : new CottonTransferStagedFileSnapshot(transferId, file.Name, file.FullName, file.Length);
        }

        private static string CreateTransferDirectory(string rootDirectory, Guid transferId)
        {
            return Path.Combine(rootDirectory, transferId.ToString("N"));
        }

        private static string CreateTemporaryDirectory(string rootDirectory)
        {
            return Path.Combine(rootDirectory, TemporaryDirectoryName);
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
