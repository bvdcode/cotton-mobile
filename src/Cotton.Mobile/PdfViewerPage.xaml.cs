// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Controls;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class PdfViewerPage : DocumentViewerPage
    {
        private bool _didLoad;

        public PdfViewerPage(PdfViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_didLoad || BindingContext is not PdfViewerViewModel viewModel)
            {
                return;
            }

            _didLoad = true;
            viewModel.LoadCommand.Execute(null);
        }
    }
}
