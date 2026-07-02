// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Controls;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class TextViewerPage : DocumentViewerPage
    {
        public TextViewerPage(TextViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
