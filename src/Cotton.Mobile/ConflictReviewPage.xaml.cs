// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class ConflictReviewPage : ContentPage
    {
        private readonly ConflictReviewViewModel _viewModel;
        private bool _loaded;

        public ConflictReviewPage(ConflictReviewViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_loaded)
            {
                _loaded = true;
                await _viewModel.RefreshCommand.ExecuteAsync(null);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            _viewModel.Close();
            return true;
        }
    }
}
