// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile
{
    public partial class App : Application
    {
        private readonly Func<AppShell> _appShellFactory;

        public App(Func<AppShell> appShellFactory)
        {
            ArgumentNullException.ThrowIfNull(appShellFactory);

            InitializeComponent();
            _appShellFactory = appShellFactory;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShellFactory());
        }
    }
}
