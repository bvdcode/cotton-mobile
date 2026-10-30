// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;

namespace Cotton.Mobile.Platforms.Android
{
    internal class PendingDocumentTreePick(
        TaskCompletionSource<Intent?> completion,
        CancellationTokenRegistration cancellationRegistration) : IDisposable
    {
        private readonly CancellationTokenRegistration _cancellationRegistration = cancellationRegistration;

        public TaskCompletionSource<Intent?> Completion { get; } = completion;

        public void Dispose()
        {
            _cancellationRegistration.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
#endif
