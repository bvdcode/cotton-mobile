// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public partial class TopAppBar : ContentView
    {
        private const string DarkActionIconButtonStyleResourceKey = "M3DarkTopAppBarIconButton";
        private const string DarkSurfaceStyleResourceKey = "M3DarkTopAppBarSurface";
        private const string DarkTitleStyleResourceKey = "M3DarkAppBarTitleLine";
        private const string DefaultActionClusterStyleResourceKey = "M3TopAppBarActionCluster";
        private const string DefaultActionIconButtonStyleResourceKey = "M3TopAppBarIconButton";
        private const string DefaultContentGridStyleResourceKey = "M3TopAppBarContentGrid";
        private const string DefaultSurfaceStyleResourceKey = "M3TopAppBarSurface";
        private const string DefaultTitleStyleResourceKey = "M3AppBarTitleLine";

        public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
            nameof(TitleText),
            typeof(string),
            typeof(TopAppBar),
            string.Empty);

        public static readonly BindableProperty UseDarkThemeProperty = BindableProperty.Create(
            nameof(UseDarkTheme),
            typeof(bool),
            typeof(TopAppBar),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryIconDataProperty = BindableProperty.Create(
            nameof(PrimaryIconData),
            typeof(Geometry),
            typeof(TopAppBar),
            default(Geometry));

        public static readonly BindableProperty PrimaryCommandProperty = BindableProperty.Create(
            nameof(PrimaryCommand),
            typeof(ICommand),
            typeof(TopAppBar));

        public static readonly BindableProperty PrimaryDescriptionProperty = BindableProperty.Create(
            nameof(PrimaryDescription),
            typeof(string),
            typeof(TopAppBar),
            "Primary action");

        public static readonly BindableProperty IsPrimaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsPrimaryActionVisible),
            typeof(bool),
            typeof(TopAppBar),
            true);

        public static readonly BindableProperty SecondaryIconDataProperty = BindableProperty.Create(
            nameof(SecondaryIconData),
            typeof(Geometry),
            typeof(TopAppBar),
            default(Geometry));

        public static readonly BindableProperty SecondaryCommandProperty = BindableProperty.Create(
            nameof(SecondaryCommand),
            typeof(ICommand),
            typeof(TopAppBar));

        public static readonly BindableProperty SecondaryDescriptionProperty = BindableProperty.Create(
            nameof(SecondaryDescription),
            typeof(string),
            typeof(TopAppBar),
            "Secondary action");

        public static readonly BindableProperty IsSecondaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsSecondaryActionVisible),
            typeof(bool),
            typeof(TopAppBar),
            true);

        public static readonly BindableProperty TertiaryIconDataProperty = BindableProperty.Create(
            nameof(TertiaryIconData),
            typeof(Geometry),
            typeof(TopAppBar),
            default(Geometry));

        public static readonly BindableProperty TertiaryCommandProperty = BindableProperty.Create(
            nameof(TertiaryCommand),
            typeof(ICommand),
            typeof(TopAppBar));

        public static readonly BindableProperty TertiaryDescriptionProperty = BindableProperty.Create(
            nameof(TertiaryDescription),
            typeof(string),
            typeof(TopAppBar),
            "Tertiary action");

        public static readonly BindableProperty IsTertiaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsTertiaryActionVisible),
            typeof(bool),
            typeof(TopAppBar),
            false);

        public TopAppBar()
        {
            BackCommand = new Command(ExecuteBackCommand);
            InitializeComponent();
            UpdateVisualState();
        }

        public ICommand BackCommand { get; }

        public string TitleText
        {
            get => (string)GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        public bool UseDarkTheme
        {
            get => (bool)GetValue(UseDarkThemeProperty);
            set => SetValue(UseDarkThemeProperty, value);
        }

        public Geometry? PrimaryIconData
        {
            get => (Geometry?)GetValue(PrimaryIconDataProperty);
            set => SetValue(PrimaryIconDataProperty, value);
        }

        public ICommand? PrimaryCommand
        {
            get => (ICommand?)GetValue(PrimaryCommandProperty);
            set => SetValue(PrimaryCommandProperty, value);
        }

        public string PrimaryDescription
        {
            get => (string)GetValue(PrimaryDescriptionProperty);
            set => SetValue(PrimaryDescriptionProperty, value);
        }

        public bool IsPrimaryActionVisible
        {
            get => (bool)GetValue(IsPrimaryActionVisibleProperty);
            set => SetValue(IsPrimaryActionVisibleProperty, value);
        }

        public Geometry? SecondaryIconData
        {
            get => (Geometry?)GetValue(SecondaryIconDataProperty);
            set => SetValue(SecondaryIconDataProperty, value);
        }

        public ICommand? SecondaryCommand
        {
            get => (ICommand?)GetValue(SecondaryCommandProperty);
            set => SetValue(SecondaryCommandProperty, value);
        }

        public string SecondaryDescription
        {
            get => (string)GetValue(SecondaryDescriptionProperty);
            set => SetValue(SecondaryDescriptionProperty, value);
        }

        public bool IsSecondaryActionVisible
        {
            get => (bool)GetValue(IsSecondaryActionVisibleProperty);
            set => SetValue(IsSecondaryActionVisibleProperty, value);
        }

        public Geometry? TertiaryIconData
        {
            get => (Geometry?)GetValue(TertiaryIconDataProperty);
            set => SetValue(TertiaryIconDataProperty, value);
        }

        public ICommand? TertiaryCommand
        {
            get => (ICommand?)GetValue(TertiaryCommandProperty);
            set => SetValue(TertiaryCommandProperty, value);
        }

        public string TertiaryDescription
        {
            get => (string)GetValue(TertiaryDescriptionProperty);
            set => SetValue(TertiaryDescriptionProperty, value);
        }

        public bool IsTertiaryActionVisible
        {
            get => (bool)GetValue(IsTertiaryActionVisibleProperty);
            set => SetValue(IsTertiaryActionVisibleProperty, value);
        }

        private async void ExecuteBackCommand()
        {
            INavigation navigation = Shell.Current.Navigation;
            await navigation.PopAsync();
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TopAppBar view = (TopAppBar)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string surfaceStyleResourceKey = UseDarkTheme
                ? DarkSurfaceStyleResourceKey
                : DefaultSurfaceStyleResourceKey;
            string titleStyleResourceKey = UseDarkTheme
                ? DarkTitleStyleResourceKey
                : DefaultTitleStyleResourceKey;
            string actionIconButtonStyleResourceKey = UseDarkTheme
                ? DarkActionIconButtonStyleResourceKey
                : DefaultActionIconButtonStyleResourceKey;

            SetDynamicResource(StyleProperty, surfaceStyleResourceKey);
            ContentGrid.SetDynamicResource(StyleProperty, DefaultContentGridStyleResourceKey);
            BackButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);
            TitleLabel.SetDynamicResource(Label.StyleProperty, titleStyleResourceKey);
            Actions.ClusterStyleResourceKey = DefaultActionClusterStyleResourceKey;
            Actions.PrimaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            Actions.SecondaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            Actions.TertiaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
        }
    }
}
