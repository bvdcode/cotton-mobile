// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public partial class SyncRootModeOptionView : CommandPressableContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SyncRootModeOptionView),
            string.Empty,
            propertyChanged: OnSemanticPropertyChanged);

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
            typeof(string),
            typeof(SyncRootModeOptionView),
            string.Empty);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(SyncRootModeOptionView),
            default(Geometry));

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(SyncRootModeOptionView),
            false,
            propertyChanged: OnSemanticPropertyChanged);

        public SyncRootModeOptionView()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string SupportingText
        {
            get => (string)GetValue(SupportingTextProperty);
            set => SetValue(SupportingTextProperty, value);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private static void OnSemanticPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SyncRootModeOptionView view = (SyncRootModeOptionView)bindable;
            SemanticProperties.SetDescription(
                view,
                SyncRootSetupResources.CreateModeDescription(view.Title, view.IsSelected));
        }
    }
}
