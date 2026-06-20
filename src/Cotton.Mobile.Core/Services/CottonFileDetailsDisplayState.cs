namespace Cotton.Mobile.Services
{
    public class CottonFileDetailsDisplayState
    {
        private const string UnknownText = "Unknown";

        private CottonFileDetailsDisplayState(
            string title,
            string kindText,
            string sizeText,
            string updatedText,
            string contentTypeText,
            string onDeviceText)
        {
            Title = title;
            KindText = kindText;
            SizeText = sizeText;
            UpdatedText = updatedText;
            ContentTypeText = contentTypeText;
            OnDeviceText = onDeviceText;
            Message = string.Join(
                Environment.NewLine,
                $"Type: {KindText}",
                $"Size: {SizeText}",
                $"Updated: {UpdatedText}",
                $"Saved on this device: {OnDeviceText}");
        }

        public string Title { get; }

        public string KindText { get; }

        public string SizeText { get; }

        public string UpdatedText { get; }

        public string ContentTypeText { get; }

        public string OnDeviceText { get; }

        public string Message { get; }

        public static CottonFileDetailsDisplayState Create(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot? localFile,
            TimeZoneInfo displayTimeZone)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(displayTimeZone);

            string sizeText = file.SizeBytes.HasValue
                ? CottonFileSizeFormatter.Format(file.SizeBytes.Value)
                : UnknownText;
            string contentTypeText = file.ContentType ?? UnknownText;
            string updatedText = FormatTimestamp(file.UpdatedAtUtc, displayTimeZone);
            string onDeviceText = CreateLocalCopyText(file, localFile);

            return new CottonFileDetailsDisplayState(
                file.Name,
                file.Kind,
                sizeText,
                updatedText,
                contentTypeText,
                onDeviceText);
        }

        private static string FormatTimestamp(DateTime value, TimeZoneInfo displayTimeZone)
        {
            DateTime utc = CottonLocalFileFreshness.NormalizeUtc(value);
            DateTime displayTime = TimeZoneInfo.ConvertTimeFromUtc(utc, displayTimeZone);
            return $"{displayTime:yyyy-MM-dd HH:mm}";
        }

        private static string CreateLocalCopyText(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot? localFile)
        {
            if (localFile is null)
            {
                return "Not saved";
            }

            string localSize = CottonFileSizeFormatter.Format(localFile.SizeBytes);
            if (file.SizeBytes.HasValue && file.SizeBytes.Value != localFile.SizeBytes)
            {
                return $"Needs refresh ({localSize})";
            }

            if (!CottonLocalFileFreshness.IsFresh(localFile.UpdatedAtUtc, file.UpdatedAtUtc))
            {
                return $"Needs refresh ({localSize})";
            }

            return "Saved";
        }
    }
}
