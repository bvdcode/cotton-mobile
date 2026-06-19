using Android.Content;
using Android.Database;
using Android.Provider;
using Java.Lang;
using Microsoft.Extensions.Logging;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidShareIntentStagingService : IAndroidShareIntentStagingService
    {
        private const string MissingPermissionMessage = "Android revoked access to the shared content.";
        private const string UnsupportedContentMessage = "Android could not open the shared content.";

        private readonly ICottonShareIntakeStore _store;
        private readonly ICottonShareContentStagingStore _contentStagingStore;
        private readonly ICottonShareLaunchState _shareLaunchState;
        private readonly ILogger<AndroidShareIntentStagingService> _logger;
        private readonly TimeProvider _timeProvider;

        public AndroidShareIntentStagingService(
            ICottonShareIntakeStore store,
            ICottonShareContentStagingStore contentStagingStore,
            ICottonShareLaunchState shareLaunchState,
            ILogger<AndroidShareIntentStagingService> logger,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(contentStagingStore);
            ArgumentNullException.ThrowIfNull(shareLaunchState);
            ArgumentNullException.ThrowIfNull(logger);

            _store = store;
            _contentStagingStore = contentStagingStore;
            _shareLaunchState = shareLaunchState;
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<CottonShareIntakeSnapshot?> StageAsync(
            Intent intent,
            ContentResolver? contentResolver,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(intent);

            if (intent.Action is not Intent.ActionSend and not Intent.ActionSendMultiple)
            {
                return null;
            }

            CottonShareIntakeKind kind = intent.Action == Intent.ActionSendMultiple
                ? CottonShareIntakeKind.SendMultiple
                : CottonShareIntakeKind.Send;
            Guid intakeId = Guid.NewGuid();
            var extraction = new ShareItemExtraction();
            await AddStreamExtrasAsync(
                intakeId,
                intent,
                contentResolver,
                extraction,
                cancellationToken).ConfigureAwait(false);
            await AddClipDataAsync(
                intakeId,
                intent,
                contentResolver,
                extraction,
                cancellationToken).ConfigureAwait(false);
            AddTextExtra(intent, extraction);

            if (extraction.Items.Count == 0)
            {
                _logger.LogInformation("Ignored Android share intent without URI or text content.");
                return null;
            }

            DateTime receivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            CottonShareIntakeSnapshot snapshot = extraction.MissingPermission
                ? CottonShareIntakeSnapshot.CreateProblem(
                    intakeId,
                    kind,
                    CottonShareIntakeStatus.MissingPermission,
                    intent.Type,
                    extraction.Items,
                    MissingPermissionMessage,
                    receivedAtUtc)
                : extraction.UnsupportedContent
                    ? CottonShareIntakeSnapshot.CreateProblem(
                        intakeId,
                        kind,
                        CottonShareIntakeStatus.Unsupported,
                        intent.Type,
                        extraction.Items,
                        UnsupportedContentMessage,
                        receivedAtUtc)
                : CottonShareIntakeSnapshot.CreatePending(
                    intakeId,
                    kind,
                    intent.Type,
                    extraction.Items,
                    receivedAtUtc);

            await _store.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
            _shareLaunchState.NotifyShareStaged();
            return snapshot;
        }

        private async Task AddStreamExtrasAsync(
            Guid intakeId,
            Intent intent,
            ContentResolver? contentResolver,
            ShareItemExtraction extraction,
            CancellationToken cancellationToken)
        {
#pragma warning disable CS0618, CA1422
            if (intent.Action == Intent.ActionSend)
            {
                if (intent.GetParcelableExtra(Intent.ExtraStream) is AndroidUri uri)
                {
                    await AddUriAsync(
                        intakeId,
                        uri,
                        intent.Type,
                        contentResolver,
                        extraction,
                        cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            System.Collections.IList? streams = intent.GetParcelableArrayListExtra(Intent.ExtraStream);
            if (streams is null)
            {
                return;
            }

            foreach (object? stream in streams)
            {
                if (stream is AndroidUri uri)
                {
                    await AddUriAsync(
                        intakeId,
                        uri,
                        intent.Type,
                        contentResolver,
                        extraction,
                        cancellationToken).ConfigureAwait(false);
                }
            }
#pragma warning restore CS0618, CA1422
        }

        private async Task AddClipDataAsync(
            Guid intakeId,
            Intent intent,
            ContentResolver? contentResolver,
            ShareItemExtraction extraction,
            CancellationToken cancellationToken)
        {
            ClipData? clipData = intent.ClipData;
            if (clipData is null)
            {
                return;
            }

            for (int index = 0; index < clipData.ItemCount; index++)
            {
                ClipData.Item? item = clipData.GetItemAt(index);
                if (item?.Uri is AndroidUri uri)
                {
                    await AddUriAsync(
                        intakeId,
                        uri,
                        intent.Type,
                        contentResolver,
                        extraction,
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                string? text = item?.Text?.ToString();
                AddText(text, displayName: null, mimeType: "text/plain", extraction);
            }
        }

        private static void AddTextExtra(Intent intent, ShareItemExtraction extraction)
        {
            string? text = intent.GetStringExtra(Intent.ExtraText);
            string? displayName =
                intent.GetStringExtra(Intent.ExtraTitle)
                ?? intent.GetStringExtra(Intent.ExtraSubject);
            AddText(text, displayName, "text/plain", extraction);
        }

        private async Task AddUriAsync(
            Guid intakeId,
            AndroidUri uri,
            string? sourceMimeType,
            ContentResolver? contentResolver,
            ShareItemExtraction extraction,
            CancellationToken cancellationToken)
        {
            string value = uri.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!extraction.SeenValues.Add($"{CottonShareIntakeItemType.Uri}:{value}"))
            {
                return;
            }

            string? displayName = TryResolveDisplayName(uri, contentResolver, extraction);
            string? mimeType = TryResolveMimeType(uri, sourceMimeType, contentResolver, extraction);
            Guid itemId = Guid.NewGuid();
            CottonShareStagedContentSnapshot? stagedContent =
                await TryStageUriContentAsync(
                    intakeId,
                    itemId,
                    uri,
                    displayName,
                    contentResolver,
                    extraction,
                    cancellationToken).ConfigureAwait(false);
            extraction.Items.Add(
                new CottonShareIntakeItemSnapshot(
                    itemId,
                    CottonShareIntakeItemType.Uri,
                    value,
                    displayName,
                    mimeType,
                    stagedContent?.FileName,
                    stagedContent?.Path,
                    stagedContent?.SizeBytes));
        }

        private async Task<CottonShareStagedContentSnapshot?> TryStageUriContentAsync(
            Guid intakeId,
            Guid itemId,
            AndroidUri uri,
            string? displayName,
            ContentResolver? contentResolver,
            ShareItemExtraction extraction,
            CancellationToken cancellationToken)
        {
            if (contentResolver is null)
            {
                extraction.UnsupportedContent = true;
                return null;
            }

            try
            {
                await using Stream? input = contentResolver.OpenInputStream(uri);
                if (input is null)
                {
                    extraction.UnsupportedContent = true;
                    return null;
                }

                return await _contentStagingStore.StageAsync(
                    intakeId,
                    itemId,
                    displayName ?? uri.LastPathSegment ?? "shared-content",
                    input,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (SecurityException)
            {
                extraction.MissingPermission = true;
                return null;
            }
            catch (System.Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or IllegalArgumentException
                    or IllegalStateException
                    or UnsupportedOperationException
                    or Java.IO.FileNotFoundException
                    or Java.IO.IOException)
            {
                extraction.UnsupportedContent = true;
                return null;
            }
        }

        private static void AddText(
            string? text,
            string? displayName,
            string? mimeType,
            ShareItemExtraction extraction)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string value = text.Trim();
            if (!extraction.SeenValues.Add($"{CottonShareIntakeItemType.Text}:{value}"))
            {
                return;
            }

            extraction.Items.Add(
                new CottonShareIntakeItemSnapshot(
                    Guid.NewGuid(),
                    CottonShareIntakeItemType.Text,
                    value,
                    displayName,
                    mimeType));
        }

        private static string? TryResolveDisplayName(
            AndroidUri uri,
            ContentResolver? contentResolver,
            ShareItemExtraction extraction)
        {
            if (contentResolver is null)
            {
                return uri.LastPathSegment;
            }

            try
            {
                using ICursor? cursor = contentResolver.Query(uri, null, null, null, null);
                if (cursor is not null && cursor.MoveToFirst())
                {
                    int displayNameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                    if (displayNameIndex >= 0)
                    {
                        string? displayName = cursor.GetString(displayNameIndex);
                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            return displayName;
                        }
                    }
                }
            }
            catch (SecurityException)
            {
                extraction.MissingPermission = true;
            }
            catch (System.Exception exception)
                when (exception is IllegalArgumentException or IllegalStateException or UnsupportedOperationException)
            {
            }

            return uri.LastPathSegment;
        }

        private static string? TryResolveMimeType(
            AndroidUri uri,
            string? sourceMimeType,
            ContentResolver? contentResolver,
            ShareItemExtraction extraction)
        {
            if (contentResolver is null)
            {
                return sourceMimeType;
            }

            try
            {
                return contentResolver.GetType(uri) ?? sourceMimeType;
            }
            catch (SecurityException)
            {
                extraction.MissingPermission = true;
                return sourceMimeType;
            }
            catch (System.Exception exception)
                when (exception is IllegalArgumentException or IllegalStateException or UnsupportedOperationException)
            {
                return sourceMimeType;
            }
        }

        private class ShareItemExtraction
        {
            public List<CottonShareIntakeItemSnapshot> Items { get; } = [];

            public HashSet<string> SeenValues { get; } = new(StringComparer.Ordinal);

            public bool MissingPermission { get; set; }

            public bool UnsupportedContent { get; set; }
        }
    }
}
