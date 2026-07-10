// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class DisabledViewerSystemChromeService : IViewerSystemChromeService
    {
        public bool IsDarkViewerActive => false;

        public void SetDarkViewerActive(bool isActive)
        {
        }
    }
}
