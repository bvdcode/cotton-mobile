using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.Collections.ObjectModel;

namespace Cotton.Mobile.ViewModels
{
    public class DiagnosticsViewModel : ViewModelBase
    {
        private readonly CottonDiagnosticsContext _context;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly IStorageManagementService _storageManagementService;
        private readonly ICottonRemotePushDiagnosticsService _remotePushDiagnosticsService;
        private readonly IClipboard _clipboard;
        private readonly ILogger<DiagnosticsViewModel> _logger;
        private bool _isBusy;
        private bool _hasCacheDetails;
        private bool _hasRemotePushDetails;
        private string _headerTitleText = "Diagnostics";
        private string _headerVersionText = "Not available";
        private string? _status;

        public DiagnosticsViewModel(
            CottonDiagnosticsContext context,
            ICottonMobileApplicationMetadata metadata,
            IStorageManagementService storageManagementService,
            ICottonRemotePushDiagnosticsService remotePushDiagnosticsService,
            IClipboard clipboard,
            ILogger<DiagnosticsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(storageManagementService);
            ArgumentNullException.ThrowIfNull(remotePushDiagnosticsService);
            ArgumentNullException.ThrowIfNull(clipboard);
            ArgumentNullException.ThrowIfNull(logger);

            _context = context;
            _metadata = metadata;
            _storageManagementService = storageManagementService;
            _remotePushDiagnosticsService = remotePushDiagnosticsService;
            _clipboard = clipboard;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            CopyCommand = new AsyncCommand(CopyAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand CopyCommand { get; }

        public ObservableCollection<DiagnosticsSectionViewModel> Sections { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    CopyCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string HeaderVersionText
        {
            get => _headerVersionText;
            private set => SetProperty(ref _headerVersionText, value);
        }

        public string HeaderTitleText
        {
            get => _headerTitleText;
            private set => SetProperty(ref _headerTitleText, value);
        }

        public string? Status
        {
            get => _status;
            private set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsStatusVisible));
                }
            }
        }

        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                CottonStorageSummary? summary = await TryLoadStorageSummaryAsync();
                CottonRemotePushDiagnosticsSnapshot? remotePush = await TryLoadRemotePushDiagnosticsAsync();
                ShowDiagnostics(summary, remotePush);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile diagnostics.");
                ShowDiagnostics(null, null);
                Status = "Could not inspect local cache.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CopyAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                bool copiedWithoutCacheDetails = false;
                if (Sections.Count == 0 || !_hasCacheDetails || !_hasRemotePushDetails)
                {
                    CottonStorageSummary? summary = await TryLoadStorageSummaryAsync();
                    CottonRemotePushDiagnosticsSnapshot? remotePush = await TryLoadRemotePushDiagnosticsAsync();
                    copiedWithoutCacheDetails = summary is null;
                    ShowDiagnostics(summary, remotePush);
                }

                await CopyDiagnosticsTextAsync();
                Status = copiedWithoutCacheDetails
                    ? "Diagnostics copied without cache details."
                    : "Diagnostics copied.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to copy Cotton mobile diagnostics.");
                Status = "Could not copy diagnostics.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task CopyDiagnosticsTextAsync()
        {
            string diagnosticsText = CreateDiagnosticsText();
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _clipboard.SetTextAsync(diagnosticsText);
            });
        }

        private async Task<CottonStorageSummary?> TryLoadStorageSummaryAsync()
        {
            try
            {
                return await _storageManagementService.GetSummaryAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile diagnostics before copy.");
                return null;
            }
        }

        private async Task<CottonRemotePushDiagnosticsSnapshot?> TryLoadRemotePushDiagnosticsAsync()
        {
            try
            {
                return await _remotePushDiagnosticsService.GetSnapshotAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile remote push diagnostics.");
                return null;
            }
        }

        private void ShowDiagnostics(
            CottonStorageSummary? summary,
            CottonRemotePushDiagnosticsSnapshot? remotePush)
        {
            HeaderTitleText = "Diagnostics";
            HeaderVersionText = CreateHeaderVersionText();
            _hasCacheDetails = summary is not null;
            _hasRemotePushDetails = remotePush is not null;
            Sections.Clear();
            foreach (DiagnosticsSectionViewModel section in CreateSections(summary, remotePush))
            {
                Sections.Add(section);
            }
        }

        private IReadOnlyList<DiagnosticsSectionViewModel> CreateSections(
            CottonStorageSummary? summary,
            CottonRemotePushDiagnosticsSnapshot? remotePush)
        {
            CottonRemotePushDiagnosticsDisplayState? pushDisplay = remotePush is null
                ? null
                : CottonRemotePushDiagnosticsDisplayState.Create(remotePush);
            return
            [
                new DiagnosticsSectionViewModel(
                    "App",
                    [
                        CreateItem("Version", CreateVersionText()),
                        CreateItem("Install", _metadata.InstallChannel),
                        CreateItem("Package", _metadata.PackageName),
                        CreateItem("Network", _context.HasInternetAccess ? "Internet available" : "No internet access"),
                    ]),
                new DiagnosticsSectionViewModel(
                    "Device",
                    [
                        CreateItem("Name", _metadata.DeviceName),
                        CreateItem("OS", _metadata.OperatingSystem),
                        CreateItem("Screen", _metadata.ScreenDetails),
                    ]),
                new DiagnosticsSectionViewModel(
                    "Session",
                    [
                        CreateItem("Instance", _context.InstanceUrl),
                        CreateItem("Account", _context.ProfileName),
                        CreateItem("Screen", _context.Screen),
                        CreateItem("Files", FormatFileCounts(_context.VisibleFileCount, _context.TotalFileCount)),
                        CreateItem("Location", _context.FileLocation),
                        CreateItem("View", _context.FileViewMode),
                        CreateItem("Sort", _context.FileSortMode),
                        CreateItem("Search", _context.IsFileSearchActive ? "Active" : "Inactive"),
                        CreateItem("Status", _context.FilesStatus),
                    ]),
                new DiagnosticsSectionViewModel(
                    "Local cache",
                    [
                        CreateItem("Total", summary is null ? null : FormatStorageSummary(summary)),
                        CreateItem("Thumbnails", summary is null ? null : FormatStorageCategory(summary.ThumbnailCache)),
                        CreateItem("Folder lists", summary is null ? null : FormatStorageCategory(summary.FolderListings)),
                        CreateItem("Downloads", summary is null ? null : FormatStorageCategory(summary.DownloadedFiles)),
                        CreateItem("Pending uploads", summary is null ? null : FormatStorageCategory(summary.TransferStaging)),
                        CreateItem("Account storage", summary is null ? null : FormatCloudQuota(summary.CloudQuota)),
                    ]),
                new DiagnosticsSectionViewModel(
                    "Remote push",
                    [
                        CreateItem("Provider", pushDisplay?.ProviderText),
                        CreateItem("Platform", pushDisplay?.PlatformText),
                        CreateItem("Backend", pushDisplay?.BackendText),
                        CreateItem("Token", pushDisplay?.PlatformTokenText),
                        CreateItem("Registration", pushDisplay?.SessionRegistrationText),
                        CreateItem("Last attempt", pushDisplay?.LastAttemptText),
                        CreateItem("Reason", pushDisplay?.ReasonText),
                    ]),
            ];
        }

        private string CreateDiagnosticsText()
        {
            IEnumerable<string> sectionLines = Sections.SelectMany(
                section => section.Items.Select(item => $"{item.Label}: {item.Value}"));
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    $"App: {CreateValue(_metadata.ApplicationName)}",
                }.Concat(sectionLines));
        }

        private DiagnosticsItemViewModel CreateItem(string label, string? value)
        {
            return new DiagnosticsItemViewModel(label, CreateValue(value));
        }

        private string CreateVersionText()
        {
            string version = CreateValue(_metadata.ApplicationVersion);
            if (string.IsNullOrWhiteSpace(_metadata.ApplicationBuild))
            {
                return version;
            }

            return $"{version} ({_metadata.ApplicationBuild.Trim()})";
        }

        private string CreateHeaderVersionText()
        {
            return CreateValue(_metadata.ApplicationName);
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile diagnostics command exception.");
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
