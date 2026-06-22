// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class CaptureInboxPage : ContentPage
    {
        public CaptureInboxPage(CaptureInboxViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is not CaptureInboxViewModel viewModel)
            {
                return;
            }

            viewModel.LoadCommand.Execute(null);
        }
    }
}
