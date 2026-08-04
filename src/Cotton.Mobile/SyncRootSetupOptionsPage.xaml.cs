// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class SyncRootSetupOptionsPage : ContentPage
    {
        private readonly SyncRootSetupOptionsViewModel _viewModel;

        public SyncRootSetupOptionsPage(SyncRootSetupOptionsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            _viewModel = viewModel;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override bool OnBackButtonPressed()
        {
            _viewModel.Cancel();
            return true;
        }

        protected override void OnDisappearing()
        {
            _viewModel.Cancel();
            base.OnDisappearing();
        }
    }
}
