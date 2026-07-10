// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

#if ANDROID
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class AndroidViewerSystemChromeService : IViewerSystemChromeService
    {
        public bool IsDarkViewerActive { get; private set; }

        public void SetDarkViewerActive(bool isActive)
        {
            if (IsDarkViewerActive == isActive)
            {
                return;
            }

            IsDarkViewerActive = isActive;
            if (MainThread.IsMainThread)
            {
                RefreshSystemBars();
                return;
            }

            MainThread.BeginInvokeOnMainThread(RefreshSystemBars);
        }

        private static void RefreshSystemBars()
        {
            if (Platform.CurrentActivity is MainActivity activity)
            {
                activity.RefreshSystemBars();
            }
        }
    }
}
#endif
