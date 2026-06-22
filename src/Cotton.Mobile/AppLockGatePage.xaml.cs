// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class AppLockGatePage : ContentPage
    {
        public AppLockGatePage(AppLockGateViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}
