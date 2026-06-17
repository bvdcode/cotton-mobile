using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class FeedbackService : IFeedbackService
    {
        private const string FeedbackSubject = "Cotton Cloud mobile feedback";

        private readonly CottonMobileOptions _options;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ILauncher _launcher;

        public FeedbackService(
            CottonMobileOptions options,
            ICottonMobileApplicationMetadata metadata,
            ILauncher launcher)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(launcher);

            _options = options;
            _metadata = metadata;
            _launcher = launcher;
        }

        public Task<bool> OpenFeedbackAsync(FeedbackContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            string body = CreateFeedbackBody(context);
            var uri = new Uri(
                $"mailto:{_options.SupportEmail}?subject={Uri.EscapeDataString(FeedbackSubject)}&body={Uri.EscapeDataString(body)}");
            return _launcher.OpenAsync(uri);
        }

        private string CreateFeedbackBody(FeedbackContext context)
        {
            var lines = new List<string>
            {
                "Please describe what happened:",
                string.Empty,
                string.Empty,
                "---",
                $"App: {_metadata.ApplicationName} {_metadata.ApplicationVersion}",
                $"Device: {_metadata.DeviceName}",
                $"OS: {_metadata.OperatingSystem}",
                $"Network: {(context.HasInternetAccess ? "Internet available" : "No internet access")}",
                $"Instance: {CreateValue(context.InstanceUrl)}",
                $"Account: {CreateValue(context.ProfileName)}",
                $"Screen: {context.Screen}",
                $"Files: {FormatFileCounts(context.VisibleFileCount, context.TotalFileCount)}",
                $"File location: {context.FileLocation}",
                $"File view: {context.FileViewMode}",
                $"File sort: {context.FileSortMode}",
                $"File search: {(context.IsFileSearchActive ? "Active" : "Inactive")}",
                $"File status: {CreateValue(context.FilesStatus)}",
            };

            if (context.StorageSummary is null)
            {
                lines.Add("Local cache: Not available");
            }
            else
            {
                lines.Add($"Local cache: {FormatStorageSummary(context.StorageSummary)}");
                lines.Add($"Thumbnail cache: {FormatStorageCategory(context.StorageSummary.ThumbnailCache)}");
                lines.Add($"Downloaded files: {FormatStorageCategory(context.StorageSummary.DownloadedFiles)}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string CreateValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Not available" : value.Trim();
        }

        private static string FormatFileCounts(int visibleFileCount, int totalFileCount)
        {
            if (visibleFileCount == totalFileCount)
            {
                return FormatFileCount(totalFileCount);
            }

            return $"{FormatFileCount(visibleFileCount)} visible of {FormatFileCount(totalFileCount)}";
        }

        private static string FormatStorageSummary(CottonStorageSummary summary)
        {
            return $"{FormatStorageSize(summary.TotalSizeBytes)} · {FormatFileCount(summary.TotalFileCount)}";
        }

        private static string FormatStorageCategory(CottonStorageCategorySnapshot category)
        {
            return $"{FormatStorageSize(category.SizeBytes)} · {FormatFileCount(category.FileCount)}";
        }

        private static string FormatFileCount(int fileCount)
        {
            return fileCount == 1 ? "1 file" : $"{fileCount:N0} files";
        }

        private static string FormatStorageSize(long bytes)
        {
            const long Kilobyte = 1024;
            const long Megabyte = Kilobyte * 1024;
            const long Gigabyte = Megabyte * 1024;

            return bytes switch
            {
                < Kilobyte => $"{bytes} B",
                < Megabyte => $"{bytes / (double)Kilobyte:0.#} KB",
                < Gigabyte => $"{bytes / (double)Megabyte:0.#} MB",
                _ => $"{bytes / (double)Gigabyte:0.#} GB",
            };
        }
    }
}
