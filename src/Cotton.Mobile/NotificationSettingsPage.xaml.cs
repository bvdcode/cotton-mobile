// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class NotificationSettingsPage : ContentPage
    {
        private bool _didLoad;

        public NotificationSettingsPage(NotificationSettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not NotificationSettingsViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
