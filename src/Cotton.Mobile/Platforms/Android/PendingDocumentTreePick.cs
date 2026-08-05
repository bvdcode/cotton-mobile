// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;

namespace Cotton.Mobile.Services
{
    internal class PendingDocumentTreePick : IDisposable
    {
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public PendingDocumentTreePick(
            TaskCompletionSource<Intent?> completion,
            CancellationTokenRegistration cancellationRegistration)
        {
            Completion = completion;
            _cancellationRegistration = cancellationRegistration;
        }

        public TaskCompletionSource<Intent?> Completion { get; }

        public void Dispose()
        {
            _cancellationRegistration.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
#endif
