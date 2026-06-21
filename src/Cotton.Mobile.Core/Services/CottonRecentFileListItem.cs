namespace Cotton.Mobile.Services
{
    public class CottonRecentFileListItem
    {
        public CottonRecentFileListItem(CottonRecentFileSnapshot file)
        {
            ArgumentNullException.ThrowIfNull(file);

            FileId = file.FileId;
            FileName = file.FileName;
            BadgeText = file.BadgeText;
            DetailText = CreateDetailText(file);
            LastActionText = CreateActionText(file.LastAction);
            LastUsedText = FormatDate(file.LastUsedAtUtc);
        }

        public Guid FileId { get; }

        public string FileName { get; }

        public string BadgeText { get; }

        public string DetailText { get; }

        public string LastActionText { get; }

        public string LastUsedText { get; }

        private static string CreateDetailText(CottonRecentFileSnapshot file)
        {
            string sizeAndKind = file.SizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(file.SizeBytes.Value)} · {file.Kind}"
                : file.Kind;
            return $"{sizeAndKind} · {CreateActionText(file.LastAction)} {FormatDate(file.LastUsedAtUtc)}";
        }

        private static string CreateActionText(CottonRecentFileActionKind action)
        {
            return action switch
            {
                CottonRecentFileActionKind.Opened => "Opened",
                CottonRecentFileActionKind.Downloaded => "Downloaded",
                CottonRecentFileActionKind.Shared => "Shared",
                _ => throw new ArgumentOutOfRangeException(nameof(action), "Recent file action is unknown."),
            };
        }

        private static string FormatDate(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return $"{utc:yyyy-MM-dd HH:mm} UTC";
        }
    }
}
