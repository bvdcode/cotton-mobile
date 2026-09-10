// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.Content;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Platforms.Android
{
    [BroadcastReceiver(Name = ComponentName, Enabled = true, Exported = true, Permission = "android.permission.DUMP")]
    public class AndroidUploadUiScenarioReceiver : BroadcastReceiver
    {
        public const string ComponentName = "dev.cottoncloud.app.debug.UploadUiScenarioReceiver";
        private const string LogTag = "CottonUploadUiTests";
        private const string AccountScope = "upload-ui-tests";
        private static readonly Uri InstanceUri = new("https://upload-test.invalid");
        private static readonly Guid RootId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        private static readonly Guid FolderId = Guid.Parse("a0000000-0000-0000-0000-000000000002");

        public override void OnReceive(Context? context, Intent? intent)
        {
            PendingResult pending = GoAsync()
                ?? throw new InvalidOperationException("UI scenario broadcast is unavailable.");
            _ = RunAsync(intent, pending);
        }

        private static async Task RunAsync(Intent? intent, PendingResult pending)
        {
            string requestId = intent?.GetStringExtra("request-id") ?? string.Empty;
            try
            {
                string scenario = intent?.GetStringExtra("scenario")
                    ?? throw new InvalidDataException("UI scenario is required.");
                IServiceProvider services = IPlatformApplication.Current?.Services
                    ?? throw new InvalidOperationException("Application services are unavailable.");
                await MainThread.InvokeOnMainThreadAsync(() => ShowAsync(services, scenario));
                _ = global::Android.Util.Log.Info(LogTag, $"{requestId}:passed:{scenario}");
            }
            catch (Exception exception)
            {
                _ = global::Android.Util.Log.Error(LogTag, $"{requestId}:failed:{exception}");
            }
            finally
            {
                pending.Finish();
            }
        }

        private static async Task ShowAsync(IServiceProvider services, string scenario)
        {
            MainPageViewModel viewModel = services.GetRequiredService<MainPageViewModel>();
            INavigation navigation = services.GetRequiredService<AppShell>().Navigation;
            while (navigation.ModalStack.Count > 0)
            {
                await navigation.PopModalAsync(animated: false);
            }

            CottonSyncProgressHub progress = services.GetRequiredService<CottonSyncProgressHub>();
            progress.Complete(RootId);
            viewModel.Sync.Clear();
            viewModel.Sync.Configure(InstanceUri, AccountScope);
            viewModel.Display.ShowAuthenticated(
                new MainPageProfile("Test account", null, InstanceUri.Host, AccountScope, avatarUrl: null), null);
            viewModel.Display.ShowDestination(AppNavigationDestination.Sync);
            ISyncSettingsViewState state = viewModel.Sync;
            state.IsBusy = false;
            state.ShowRoots(new SyncRootCollectionSnapshot([], new HashSet<Guid>(),
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>()));

            switch (scenario)
            {
                case "offline-add":
                    EnsureOffline(services);
                    await viewModel.Sync.AddRootCommand.ExecuteAsync(null);
                    EnsureStatus(viewModel.Sync, AppResources.SyncFolderAddOffline);
                    break;
                case "offline-run":
                    EnsureOffline(services);
                    ShowRoot(state);
                    await viewModel.Sync.RunAllCommand.ExecuteAsync(null);
                    EnsureStatus(viewModel.Sync, CottonSyncSettingsRunStatusText.OfflineUnavailableStatus);
                    break;
                case "running":
                    ShowRoot(state);
                    state.IsBusy = true;
                    progress.Report(CottonSyncProgressSnapshot.ApplyingChanges(RootId, 4, 10));
                    if (!viewModel.Sync.PauseRootCommand.CanExecute(viewModel.Sync.Roots[0].PauseResumeAction))
                    {
                        throw new InvalidOperationException("Pause is unavailable during a manual upload.");
                    }

                    break;
                case "source-folder":
                case "source-media":
                    await ShowSourceAsync(navigation, scenario);
                    break;
                case "storage-full":
                    ShowRoot(state, CottonAutomaticSyncFailureKind.InsufficientStorage);
                    break;
                case "destination-missing":
                    ShowRoot(state, CottonAutomaticSyncFailureKind.RemoteContentUnavailable);
                    break;
                case "review-required":
                    ShowRoot(state, CottonAutomaticSyncFailureKind.UploadedFileChanged);
                    break;
                case "cloud-path-conflict":
                    ShowRoot(state, CottonAutomaticSyncFailureKind.RemotePathConflict);
                    break;
                case "pending-upload-changed":
                    ShowRoot(state, CottonAutomaticSyncFailureKind.PendingUploadChanged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "UI scenario is not supported.");
            }
        }

        private static async Task ShowSourceAsync(INavigation navigation, string scenario)
        {
            SyncRootSetupOptionsViewModel source = new(_ => { });
            switch (scenario)
            {
                case "source-folder":
                    source.SelectFolderCommand.Execute(null);
                    break;
                case "source-media":
                    source.SelectMediaCommand.Execute(null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }

            await navigation.PushModalAsync(new SyncRootSetupOptionsPage(source), animated: false);
        }

        private static void ShowRoot(
            ISyncSettingsViewState state,
            CottonAutomaticSyncFailureKind? failureKind = null)
        {
            CottonSyncRootSnapshot root = new(RootId, InstanceUri, AccountScope,
                new CottonUploadDestinationSnapshot(FolderId, "Camera backups",
                    "Files / Family archive / Camera backups with a long folder name"),
                new CottonSyncLocalRootSnapshot(CottonSyncRootStorageKind.MediaStore,
                    "content://media/external/file", "Camera", CottonSyncRootPermissionStatus.Available, "buckets:1"),
                CottonSyncDirection.DeviceToCloud, CottonUploadOriginalRetention.KeepOriginals);
            Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses = [];
            if (failureKind.HasValue)
            {
                statuses.Add(RootId, CottonAutomaticSyncRootStatusSnapshot.Failed(
                    RootId, DateTime.UtcNow, failureKind.Value));
            }

            state.ShowRoots(new SyncRootCollectionSnapshot([root], new HashSet<Guid>(), statuses));
        }

        private static void EnsureOffline(IServiceProvider services)
        {
            if (services.GetRequiredService<INetworkAccessService>().HasInternetAccess)
            {
                throw new InvalidOperationException("Offline UI scenarios require the emulator network to be disabled.");
            }
        }

        private static void EnsureStatus(SyncSettingsViewModel viewModel, string expected)
        {
            if (!viewModel.IsStatusVisible || viewModel.Status != expected)
            {
                throw new InvalidOperationException("The upload action did not expose its status to the view.");
            }
        }
    }
}
#endif
