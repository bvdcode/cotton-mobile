// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FeedbackService : IFeedbackService
    {
        private const string FeedbackSubject = "Cotton Cloud mobile feedback";

        private readonly CottonMobileOptions _options;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ILauncher _launcher;
        private readonly IClipboard _clipboard;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            CottonMobileOptions options,
            ICottonMobileApplicationMetadata metadata,
            ILauncher launcher,
            IClipboard clipboard,
            ILogger<FeedbackService> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(launcher);
            ArgumentNullException.ThrowIfNull(clipboard);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options;
            _metadata = metadata;
            _launcher = launcher;
            _clipboard = clipboard;
            _logger = logger;
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
            if (await OpenFeedbackComposerAsync(uri, cancellationToken))
            {
                return FeedbackDeliveryResult.ComposerOpened;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await CopyFeedbackTextAsync(body, cancellationToken);
            return FeedbackDeliveryResult.CopiedToClipboard;
        }

        public Task CopyFeedbackAsync(FeedbackContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            return CopyFeedbackTextAsync(CreateFeedbackBody(context), cancellationToken);
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
                $"Install: {_metadata.InstallChannel}",
                $"Package: {CreateValue(_metadata.PackageName)}",
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
                lines.Add($"Folder listings: {FormatStorageCategory(context.StorageSummary.FolderListings)}");
                lines.Add($"Downloaded files: {FormatStorageCategory(context.StorageSummary.DownloadedFiles)}");
                lines.Add($"Pending uploads: {FormatStorageCategory(context.StorageSummary.TransferStaging)}");
                lines.Add($"Account storage: {FormatCloudQuota(context.StorageSummary.CloudQuota)}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private Task CopyFeedbackTextAsync(string body, CancellationToken cancellationToken)
        {
            string text = string.Join(
                Environment.NewLine,
                $"To: {_options.SupportEmail}",
                $"Subject: {FeedbackSubject}",
                string.Empty,
                body);
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _clipboard.SetTextAsync(text);
            });
        }

        private async Task<bool> OpenFeedbackComposerAsync(Uri uri, CancellationToken cancellationToken)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await _launcher.OpenAsync(uri);
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton Cloud feedback composer.");
                return false;
            }
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
            return $"{CottonFileSizeFormatter.Format(summary.TotalSizeBytes)} · {FormatFileCount(summary.TotalFileCount)}";
        }

        private static string FormatStorageCategory(CottonStorageCategorySnapshot category)
        {
            return $"{CottonFileSizeFormatter.Format(category.SizeBytes)} · {FormatFileCount(category.FileCount)}";
        }

        private static string FormatCloudQuota(CottonCloudStorageQuotaSnapshot quota)
        {
            return CottonCloudStorageQuotaDiagnosticText.Create(quota);
        }

        private static string FormatFileCount(int fileCount)
        {
            return fileCount == 1 ? "1 file" : $"{fileCount:N0} files";
        }

    }
}
