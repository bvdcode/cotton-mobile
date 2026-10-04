// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Database;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidCursorScope(ICursor? cursor) : IDisposable
    {
        private bool disposed;

        public ICursor? Cursor { get; } = cursor;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            GC.SuppressFinalize(this);
            if (Cursor is null)
            {
                return;
            }

            try
            {
                Cursor.Close();
            }
            finally
            {
                Cursor.Dispose();
            }
        }
    }
}
#endif
