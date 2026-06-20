using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public class CottonFileVersionListSnapshot
    {
        private CottonFileVersionListSnapshot(
            CottonFileVersionListStatus status,
            string title,
            string summaryText,
            string emptyText,
            IReadOnlyList<CottonFileVersionItemSnapshot> items)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "File version list status is unknown.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("File version list title is required.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(summaryText))
            {
                throw new ArgumentException("File version list summary is required.", nameof(summaryText));
            }

            Status = status;
            Title = title.Trim();
            SummaryText = summaryText.Trim();
            EmptyText = string.IsNullOrWhiteSpace(emptyText) ? "No versions found." : emptyText.Trim();
            Items = items;
        }

        public CottonFileVersionListStatus Status { get; }

        public string Title { get; }

        public string SummaryText { get; }

        public string EmptyText { get; }

        public IReadOnlyList<CottonFileVersionItemSnapshot> Items { get; }

        public bool HasItems => Items.Count > 0;

        public static CottonFileVersionListSnapshot Create(
            string fileName,
            IEnumerable<FileVersionDto> versions,
            TimeZoneInfo displayTimeZone)
        {
            ArgumentNullException.ThrowIfNull(versions);
            ArgumentNullException.ThrowIfNull(displayTimeZone);

            string normalizedFileName = string.IsNullOrWhiteSpace(fileName)
                ? "file"
                : fileName.Trim();
            CottonFileVersionItemSnapshot[] items = versions
                .Select(version => CottonFileVersionItemSnapshot.Create(version, displayTimeZone))
                .OrderByDescending(version => version.IsCurrent)
                .ThenByDescending(version => version.VersionNumber)
                .ThenByDescending(version => version.UpdatedText, StringComparer.Ordinal)
                .ToArray();

            if (items.Length == 0)
            {
                return new CottonFileVersionListSnapshot(
                    CottonFileVersionListStatus.Empty,
                    "Version history",
                    $"No versions found for {normalizedFileName}.",
                    "No versions found.",
                    items);
            }

            return new CottonFileVersionListSnapshot(
                CottonFileVersionListStatus.Ready,
                "Version history",
                CreateSummary(normalizedFileName, items.Length),
                "No versions found.",
                items);
        }

        private static string CreateSummary(string fileName, int versionCount)
        {
            string countText = versionCount == 1 ? "1 version" : $"{versionCount:N0} versions";
            return $"{countText} for {fileName}.";
        }
    }
}
