// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile
{
    public partial class App : Application
    {
        private readonly Func<AppShell> _appShellFactory;

        public App(
            Func<AppShell> appShellFactory,
            ICottonNotificationSessionService notificationSessionService)
        {
            ArgumentNullException.ThrowIfNull(appShellFactory);
            ArgumentNullException.ThrowIfNull(notificationSessionService);

            InitializeComponent();
            _appShellFactory = appShellFactory;
            notificationSessionService.Initialize();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShellFactory());
        }
    }
}
