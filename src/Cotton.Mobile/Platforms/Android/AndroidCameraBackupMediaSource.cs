// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidCameraBackupMediaSource :
        ICottonCameraBackupMediaSource,
        ICottonCameraBackupMediaContentSource
    {
        private const string DefaultImageContentType = "image/jpeg";
        private const string DefaultVideoContentType = "video/mp4";

        private const int IdColumn = 0;
        private const int DisplayNameColumn = 1;
        private const int MimeTypeColumn = 2;
        private const int SizeColumn = 3;
        private const int DateModifiedColumn = 4;
        private const int DateTakenColumn = 5;

        private static readonly string[] BaseProjection =
        [
            IBaseColumns.Id,
            MediaStore.IMediaColumns.DisplayName,
            MediaStore.IMediaColumns.MimeType,
            MediaStore.IMediaColumns.Size,
            MediaStore.IMediaColumns.DateModified,
        ];

        private readonly ICottonCameraBackupMediaAccessPolicy _accessPolicy;

        public AndroidCameraBackupMediaSource(ICottonCameraBackupMediaAccessPolicy accessPolicy)
        {
            ArgumentNullException.ThrowIfNull(accessPolicy);

            _accessPolicy = accessPolicy;
        }

        public async Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
            CancellationToken cancellationToken = default)
        {
            CottonCameraBackupMediaAccessState accessState =
                await _accessPolicy.GetAccessStateAsync(cancellationToken).ConfigureAwait(false);
            if (!CottonCameraBackupMediaAccessRules.CanScanFullLibrary(accessState))
            {
                return Array.Empty<CottonCameraBackupCandidate>();
            }

            cancellationToken.ThrowIfCancellationRequested();
            ContentResolver? resolver = Android.App.Application.Context.ContentResolver;
            if (resolver is null)
            {
                return Array.Empty<CottonCameraBackupCandidate>();
            }

            var candidates = new List<CottonCameraBackupCandidate>();
            AddCandidates(
                candidates,
                resolver,
                MediaStore.Images.Media.ExternalContentUri,
                DefaultImageContentType,
                cancellationToken);
            AddCandidates(
                candidates,
                resolver,
                MediaStore.Video.Media.ExternalContentUri,
                DefaultVideoContentType,
                cancellationToken);

            return candidates;
        }

        public Task<Stream?> OpenReadAsync(
            CottonCameraBackupCandidate candidate,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            cancellationToken.ThrowIfCancellationRequested();

            if (!Uri.TryCreate(candidate.Identity.SourceId, UriKind.Absolute, out Uri? sourceUri)
                || !string.Equals(sourceUri.Scheme, "content", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Stream?>(null);
            }

            try
            {
                AndroidUri? androidUri = AndroidUri.Parse(candidate.Identity.SourceId);
                if (androidUri is null)
                {
                    return Task.FromResult<Stream?>(null);
                }

                Stream? stream = Android.App.Application.Context.ContentResolver?.OpenInputStream(androidUri);
                return Task.FromResult(stream);
            }
            catch (Java.Lang.SecurityException)
            {
                return Task.FromResult<Stream?>(null);
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult<Stream?>(null);
            }
        }

        internal static bool TryCreateCandidate(
            CottonCameraBackupMediaSourceRecord record,
            out CottonCameraBackupCandidate? candidate)
        {
            return CottonCameraBackupMediaSourceRecordMapper.TryCreateCandidate(record, out candidate);
        }

        private static void AddCandidates(
            List<CottonCameraBackupCandidate> candidates,
            ContentResolver resolver,
            AndroidUri? collectionUri,
            string defaultContentType,
            CancellationToken cancellationToken)
        {
            if (collectionUri is null)
            {
                return;
            }

            candidates.AddRange(
                QueryCandidates(
                    resolver,
                    collectionUri,
                    defaultContentType,
                    cancellationToken));
        }

        private static IReadOnlyList<CottonCameraBackupCandidate> QueryCandidates(
            ContentResolver resolver,
            AndroidUri collectionUri,
            string defaultContentType,
            CancellationToken cancellationToken)
        {
            var candidates = new List<CottonCameraBackupCandidate>();

            try
            {
                using ICursor? cursor = resolver.Query(
                    collectionUri,
                    CreateProjection(),
                    CreateVisibleMediaSelection(),
                    null,
                    $"{MediaStore.IMediaColumns.DateModified} DESC, {IBaseColumns.Id} DESC");
                if (cursor is null)
                {
                    return candidates;
                }

                while (cursor.MoveToNext())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CottonCameraBackupMediaSourceRecord record =
                        CreateRecord(cursor, collectionUri, defaultContentType);
                    if (TryCreateCandidate(record, out CottonCameraBackupCandidate? candidate)
                        && candidate is not null)
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            catch (Java.Lang.SecurityException)
            {
                return Array.Empty<CottonCameraBackupCandidate>();
            }

            return candidates;
        }

        private static string[] CreateProjection()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                return BaseProjection;
            }

            return
            [
                ..BaseProjection,
                MediaStore.IMediaColumns.DateTaken,
            ];
        }

        private static string? CreateVisibleMediaSelection()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                return $"{MediaStore.IMediaColumns.IsPending}=0 AND {MediaStore.IMediaColumns.IsTrashed}=0";
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                return $"{MediaStore.IMediaColumns.IsPending}=0";
            }

            return null;
        }

        private static CottonCameraBackupMediaSourceRecord CreateRecord(
            ICursor cursor,
            AndroidUri collectionUri,
            string defaultContentType)
        {
            long? id = GetNullableLong(cursor, IdColumn);
            string? sourceId = id is null
                ? null
                : ContentUris.WithAppendedId(collectionUri, id.Value)?.ToString();
            string? contentType = GetNullableString(cursor, MimeTypeColumn);
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = defaultContentType;
            }

            return new CottonCameraBackupMediaSourceRecord(
                sourceId,
                GetNullableString(cursor, DisplayNameColumn),
                contentType,
                GetNullableLong(cursor, SizeColumn),
                UnixSecondsToUtc(GetNullableLong(cursor, DateModifiedColumn)),
                TryGetDateTakenUtc(cursor));
        }

        private static DateTime? TryGetDateTakenUtc(ICursor cursor)
        {
            return cursor.ColumnCount > DateTakenColumn
                ? UnixMillisecondsToUtc(GetNullableLong(cursor, DateTakenColumn))
                : null;
        }

        private static string? GetNullableString(ICursor cursor, int columnIndex)
        {
            return cursor.IsNull(columnIndex)
                ? null
                : cursor.GetString(columnIndex);
        }

        private static long? GetNullableLong(ICursor cursor, int columnIndex)
        {
            return cursor.IsNull(columnIndex)
                ? null
                : cursor.GetLong(columnIndex);
        }

        private static DateTime? UnixSecondsToUtc(long? seconds)
        {
            if (seconds is null || seconds <= 0)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(seconds.Value).UtcDateTime;
        }

        private static DateTime? UnixMillisecondsToUtc(long? milliseconds)
        {
            if (milliseconds is null || milliseconds <= 0)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds.Value).UtcDateTime;
        }
    }
}
#endif
