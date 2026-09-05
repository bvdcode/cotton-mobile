// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Microsoft.Maui.Storage;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDocumentTreeActivityResultStore(IPreferences preferences)
    {
        private const string Prefix = "Cotton.Mobile.DocumentTreeResult.";
        private const string ActiveRequestIdKey = Prefix + "ActiveRequestId";
        private const string ActiveProcessIdKey = Prefix + "ActiveProcessId";
        private const string ResultRequestIdKey = Prefix + "ResultRequestId";
        private const string ResultCodeKey = Prefix + "ResultCode";
        private const string DataUriKey = Prefix + "DataUri";
        private const string FlagsKey = Prefix + "Flags";

        private readonly IPreferences _preferences =
            preferences ?? throw new ArgumentNullException(nameof(preferences));

        public bool IsActiveInCurrentProcess(Guid requestId)
        {
            return TryReadRequestId(ActiveRequestIdKey, out Guid activeRequestId)
                && activeRequestId == requestId
                && _preferences.Get(ActiveProcessIdKey, 0) == Environment.ProcessId;
        }

        public void Begin(Guid requestId)
        {
            EnsureRequestId(requestId);
            if (TryReadRequestId(ActiveRequestIdKey, out Guid activeRequestId)
                && activeRequestId != requestId)
            {
                throw new InvalidOperationException("A different document-tree request is already active.");
            }

            _preferences.Set(ActiveRequestIdKey, requestId.ToString("N"));
            _preferences.Set(ActiveProcessIdKey, Environment.ProcessId);
        }

        public void RestoreActiveRequest()
        {
            if (TryReadRequestId(ActiveRequestIdKey, out _))
            {
                _preferences.Set(ActiveProcessIdKey, Environment.ProcessId);
            }
        }

        public Guid? SaveResult(Result resultCode, Intent? data)
        {
            if (!TryReadRequestId(ActiveRequestIdKey, out Guid requestId))
            {
                return null;
            }

            _preferences.Remove(ResultRequestIdKey);
            _preferences.Set(ResultCodeKey, (int)resultCode);
            if (data?.Data is AndroidUri uri)
            {
                _preferences.Set(DataUriKey, uri.ToString() ?? string.Empty);
                _preferences.Set(FlagsKey, (int)data.Flags);
            }
            else
            {
                _preferences.Remove(DataUriKey);
                _preferences.Remove(FlagsKey);
            }

            _preferences.Set(ResultRequestIdKey, requestId.ToString("N"));
            _preferences.Remove(ActiveRequestIdKey);
            _preferences.Remove(ActiveProcessIdKey);
            return requestId;
        }

        public bool TryReadResult(Guid requestId, out Intent? data)
        {
            EnsureRequestId(requestId);
            data = null;
            if (!TryReadRequestId(ResultRequestIdKey, out Guid resultRequestId)
                || resultRequestId != requestId)
            {
                return false;
            }

            Result resultCode = (Result)_preferences.Get(ResultCodeKey, (int)Result.Canceled);
            if (resultCode != Result.Ok)
            {
                return true;
            }

            string dataUri = _preferences.Get(DataUriKey, string.Empty);
            if (string.IsNullOrWhiteSpace(dataUri))
            {
                throw new InvalidDataException("Saved document-tree URI is unavailable.");
            }

            AndroidUri uri = AndroidUri.Parse(dataUri)
                ?? throw new InvalidDataException("Saved document-tree URI is invalid.");
            Intent resultIntent = new();
            resultIntent.SetData(uri);
            resultIntent.SetFlags((ActivityFlags)_preferences.Get(FlagsKey, 0));
            data = resultIntent;
            return true;
        }

        public void Complete(Guid requestId)
        {
            EnsureRequestId(requestId);
            if (TryReadRequestId(ActiveRequestIdKey, out Guid activeRequestId)
                && activeRequestId == requestId)
            {
                _preferences.Remove(ActiveRequestIdKey);
                _preferences.Remove(ActiveProcessIdKey);
            }

            if (TryReadRequestId(ResultRequestIdKey, out Guid resultRequestId)
                && resultRequestId == requestId)
            {
                _preferences.Remove(ResultRequestIdKey);
                _preferences.Remove(ResultCodeKey);
                _preferences.Remove(DataUriKey);
                _preferences.Remove(FlagsKey);
            }
        }

        private bool TryReadRequestId(string key, out Guid requestId)
        {
            return Guid.TryParse(_preferences.Get(key, string.Empty), out requestId)
                && requestId != Guid.Empty;
        }

        private static void EnsureRequestId(Guid requestId)
        {
            if (requestId == Guid.Empty)
            {
                throw new ArgumentException("Document-tree request id is required.", nameof(requestId));
            }
        }
    }
}
#endif
