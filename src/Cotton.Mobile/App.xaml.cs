// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile
{
    public partial class App : Application
    {
        private readonly AppShell _appShell;

        public App(AppShell appShell)
        {
            ArgumentNullException.ThrowIfNull(appShell);

            InitializeComponent();
            _appShell = appShell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShell);
        }
    }
}
