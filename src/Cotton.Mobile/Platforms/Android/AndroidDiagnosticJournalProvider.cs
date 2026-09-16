// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    [ContentProvider(["${applicationId}.diagnostics"], Exported = true, Permission = "android.permission.DUMP")]
    public class AndroidDiagnosticJournalProvider : ContentProvider
    {
        public override bool OnCreate() => true;

        public override string GetType(AndroidUri uri) => "text/plain";

        public override ParcelFileDescriptor OpenFile(AndroidUri uri, string mode)
        {
            if (!string.Equals(uri.Path, "/journal", StringComparison.Ordinal)
                || !string.Equals(mode, "r", StringComparison.Ordinal))
            {
                throw new Java.IO.FileNotFoundException("Only the diagnostic journal can be read.");
            }

            ParcelFileDescriptor[] pipe = ParcelFileDescriptor.CreateReliablePipe()
                ?? throw new Java.IO.IOException("Could not create the diagnostic journal pipe.");
            _ = WriteJournalAsync(pipe[1]);
            return pipe[0];
        }

        private static async Task WriteJournalAsync(ParcelFileDescriptor destination)
        {
            using (destination)
            using (ParcelFileDescriptor.AutoCloseOutputStream javaOutput = new(destination))
            using (OutputStreamInvoker output = new(javaOutput))
            {
                try
                {
                    IServiceProvider services = await MainThread.InvokeOnMainThreadAsync(() =>
                            IPlatformApplication.Current?.Services
                                ?? throw new InvalidOperationException("Android application services are unavailable."))
                        .ConfigureAwait(false);
                    await Task.Run(async () =>
                    {
                        IReadOnlyList<string> records = services.GetRequiredService<ICottonDiagnosticJournal>().ReadAll();
                        using StreamWriter writer = new(output, new UTF8Encoding(false), leaveOpen: true);
                        await CottonDiagnosticJournalTransfer.WriteAsync(writer, records).ConfigureAwait(false);
                        await writer.FlushAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _ = Log.Error(nameof(AndroidDiagnosticJournalProvider), exception.ToString());
                    destination.CloseWithError("Diagnostic journal export failed.");
                }
            }
        }

        public override ICursor? Query(AndroidUri uri, string[]? projection, string? selection,
            string[]? selectionArgs, string? sortOrder) => throw new Java.Lang.UnsupportedOperationException();

        public override AndroidUri? Insert(AndroidUri uri, ContentValues? values) => throw new Java.Lang.UnsupportedOperationException();

        public override int Delete(AndroidUri uri, string? selection, string[]? selectionArgs) => throw new Java.Lang.UnsupportedOperationException();

        public override int Update(AndroidUri uri, ContentValues? values, string? selection,
            string[]? selectionArgs) => throw new Java.Lang.UnsupportedOperationException();
    }
}
#endif
