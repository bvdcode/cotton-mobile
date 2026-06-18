namespace Cotton.Mobile.Services
{
    public static class CottonTemporaryFilePolicy
    {
        public static TimeSpan AbandonedFileGracePeriod { get; } = TimeSpan.FromHours(6);

        public static bool IsAbandoned(FileInfo file, DateTime utcNow)
        {
            ArgumentNullException.ThrowIfNull(file);

            return ResolveActivityTimestampUtc(file) <= utcNow - AbandonedFileGracePeriod;
        }

        public static DateTime ResolveActivityTimestampUtc(FileInfo file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return file.LastAccessTimeUtc > file.LastWriteTimeUtc
                ? file.LastAccessTimeUtc
                : file.LastWriteTimeUtc;
        }
    }
}
