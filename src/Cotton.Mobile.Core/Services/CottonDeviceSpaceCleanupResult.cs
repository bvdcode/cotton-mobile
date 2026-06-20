namespace Cotton.Mobile.Services
{
    public class CottonDeviceSpaceCleanupResult
    {
        public CottonDeviceSpaceCleanupResult(int fileCount, long sizeBytes)
        {
            if (fileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "Freed file count cannot be negative.");
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Freed size cannot be negative.");
            }

            FileCount = fileCount;
            SizeBytes = sizeBytes;
        }

        public static CottonDeviceSpaceCleanupResult Empty { get; } = new(0, 0);

        public int FileCount { get; }

        public long SizeBytes { get; }

        public bool HasDeletedFiles => FileCount > 0;

        public CottonDeviceSpaceCleanupResult Add(CottonDeviceSpaceCleanupResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            return new CottonDeviceSpaceCleanupResult(
                FileCount + result.FileCount,
                SizeBytes + result.SizeBytes);
        }
    }
}
