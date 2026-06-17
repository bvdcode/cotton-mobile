using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Cotton.Mobile.Services
{
    public class FeedbackService : IFeedbackService
    {
        private const string FeedbackSubject = "Cotton Cloud mobile feedback";

        private readonly CottonMobileOptions _options;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ILauncher _launcher;
        private readonly IClipboard _clipboard;

        public FeedbackService(
            CottonMobileOptions options,
            ICottonMobileApplicationMetadata metadata,
            ILauncher launcher,
            IClipboard clipboard)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(launcher);
            ArgumentNullException.ThrowIfNull(clipboard);

            _options = options;
            _metadata = metadata;
            _launcher = launcher;
            _clipboard = clipboard;
        }

        public async Task<FeedbackDeliveryResult> OpenFeedbackAsync(
            FeedbackContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            string body = CreateFeedbackBody(context);
            var uri = new Uri(
                $"mailto:{_options.SupportEmail}?subject={Uri.EscapeDataString(FeedbackSubject)}&body={Uri.EscapeDataString(body)}");
            if (await _launcher.OpenAsync(uri))
            {
                return FeedbackDeliveryResult.ComposerOpened;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await CopyFeedbackTextAsync(body);
            return FeedbackDeliveryResult.CopiedToClipboard;
        }

        public Task CopyFeedbackAsync(FeedbackContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            return CopyFeedbackTextAsync(CreateFeedbackBody(context));
        }

        private string CreateFeedbackBody(FeedbackContext context)
        {
            var lines = new List<string>
            {
                "Please describe what happened:",
                string.Empty,
                string.Empty,
                "---",
                $"App: {_metadata.ApplicationName} {FormatVersion()}",
                $"Package: {CreateValue(_metadata.PackageName)}",
                $"Install channel: {CreateValue(_metadata.InstallChannel)}",
                $"Device: {_metadata.DeviceName}",
                $"OS: {_metadata.OperatingSystem}",
                $"Screen: {CreateValue(_metadata.ScreenDetails)}",
                $"Network: {(context.HasInternetAccess ? "Internet available" : "No internet access")}",
                $"Instance: {CreateValue(context.InstanceUrl)}",
                $"Account: {CreateValue(context.ProfileName)}",
                $"App screen: {context.Screen}",
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

        private Task CopyFeedbackTextAsync(string body)
        {
            return _clipboard.SetTextAsync(
                string.Join(
                    Environment.NewLine,
                    $"To: {_options.SupportEmail}",
                    $"Subject: {FeedbackSubject}",
                    string.Empty,
                    body));
        }

        private static string CreateValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Not available" : value.Trim();
        }

        private string FormatVersion()
        {
            string version = CreateValue(_metadata.ApplicationVersion);
            if (string.IsNullOrWhiteSpace(_metadata.ApplicationBuild))
            {
                return version;
            }

            return $"{version} ({_metadata.ApplicationBuild.Trim()})";
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
