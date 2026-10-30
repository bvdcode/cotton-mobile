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
            ICottonNotificationSessionService notificationSessionService,
            ICottonAutomaticSyncSessionService automaticSyncSessionService)
        {
            ArgumentNullException.ThrowIfNull(appShellFactory);
            ArgumentNullException.ThrowIfNull(notificationSessionService);
            ArgumentNullException.ThrowIfNull(automaticSyncSessionService);

            InitializeComponent();
            _appShellFactory = appShellFactory;
            notificationSessionService.Initialize();
            automaticSyncSessionService.Initialize();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_appShellFactory());
        }
    }
}
