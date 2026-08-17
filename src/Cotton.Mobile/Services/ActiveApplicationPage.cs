// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class ActiveApplicationPage
    {
        public static Page GetRequired()
        {
            Application? application = Application.Current;
            if (application is null || application.Windows.Count == 0)
            {
                throw new InvalidOperationException("An active application page is required.");
            }

            return application.Windows[0].Page
                ?? throw new InvalidOperationException("An active application page is required.");
        }
    }
}
