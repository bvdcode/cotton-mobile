// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsSectionHeaderView : ContentView
    {
        private const string DefaultDetailTextStyleResourceKey = "M3CardSupportingBlock";
        private const string DefaultGridStyleResourceKey = "M3SettingsListItemGrid";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardUtilityThumbnailFrame";
        private const string DefaultTextStackStyleResourceKey = "M3CardTextStack";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextProperty = BindableProperty.Create(
            nameof(PrimaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextProperty = BindableProperty.Create(
            nameof(SecondaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextProperty = BindableProperty.Create(
            nameof(TertiaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(SettingsSectionHeaderView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsProgressVisibleProperty = BindableProperty.Create(
            nameof(IsProgressVisible),
            typeof(bool),
            typeof(SettingsSectionHeaderView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsSectionHeaderView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TertiaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _primaryDetailText;
        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly LinearProgressView _progress;
        private readonly Label _secondaryDetailText;
        private readonly Label _tertiaryDetailText;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _title;

        public SettingsSectionHeaderView()
        {
            InputTransparent = true;

            _leadingIcon = new IconFrame();
            _title = new Label();
            _primaryDetailText = new Label();
            _secondaryDetailText = new Label();
            _tertiaryDetailText = new Label();
            _progress = new LinearProgressView();
            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _title,
                    _primaryDetailText,
                    _secondaryDetailText,
                    _tertiaryDetailText,
                },
            };

            Grid.SetColumn(_textStack, 1);
            Grid.SetRow(_progress, 1);
            Grid.SetColumn(_progress, 1);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                    _progress,
                },
            };

            Content = _grid;
            UpdateVisualState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string PrimaryDetailText
        {
            get => (string)GetValue(PrimaryDetailTextProperty);
            set => SetValue(PrimaryDetailTextProperty, value);
        }

        public string SecondaryDetailText
        {
            get => (string)GetValue(SecondaryDetailTextProperty);
            set => SetValue(SecondaryDetailTextProperty, value);
        }

        public string TertiaryDetailText
        {
            get => (string)GetValue(TertiaryDetailTextProperty);
            set => SetValue(TertiaryDetailTextProperty, value);
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public bool IsProgressVisible
        {
            get => (bool)GetValue(IsProgressVisibleProperty);
            set => SetValue(IsProgressVisibleProperty, value);
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
        }

        public string TextStackStyleResourceKey
        {
            get => (string)GetValue(TextStackStyleResourceKeyProperty);
            set => SetValue(TextStackStyleResourceKeyProperty, value);
        }

        public string TitleTextStyleResourceKey
        {
            get => (string)GetValue(TitleTextStyleResourceKeyProperty);
            set => SetValue(TitleTextStyleResourceKeyProperty, value);
        }

        public string PrimaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(PrimaryDetailTextStyleResourceKeyProperty);
            set => SetValue(PrimaryDetailTextStyleResourceKeyProperty, value);
        }

        public string SecondaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(SecondaryDetailTextStyleResourceKeyProperty);
            set => SetValue(SecondaryDetailTextStyleResourceKeyProperty, value);
        }

        public string TertiaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(TertiaryDetailTextStyleResourceKeyProperty);
            set => SetValue(TertiaryDetailTextStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string title = Title ?? string.Empty;
            string primaryDetailText = PrimaryDetailText ?? string.Empty;
            string secondaryDetailText = SecondaryDetailText ?? string.Empty;
            string tertiaryDetailText = TertiaryDetailText ?? string.Empty;
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string leadingIconFrameStyleResourceKey =
                ResolveStyleResourceKey(LeadingIconFrameStyleResourceKey, DefaultLeadingIconFrameStyleResourceKey);
            string textStackStyleResourceKey =
                ResolveStyleResourceKey(TextStackStyleResourceKey, DefaultTextStackStyleResourceKey);
            string titleTextStyleResourceKey =
                ResolveStyleResourceKey(TitleTextStyleResourceKey, DefaultTitleStyleResourceKey);
            string primaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(PrimaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string secondaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(SecondaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string tertiaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(TertiaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            bool isLeadingIconVisible = LeadingIconData is not null;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = isLeadingIconVisible;
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _title.SetDynamicResource(StyleProperty, titleTextStyleResourceKey);
            _title.Text = title;
            _primaryDetailText.SetDynamicResource(StyleProperty, primaryDetailTextStyleResourceKey);
            _primaryDetailText.Text = primaryDetailText;
            _primaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(primaryDetailText);
            _secondaryDetailText.SetDynamicResource(StyleProperty, secondaryDetailTextStyleResourceKey);
            _secondaryDetailText.Text = secondaryDetailText;
            _secondaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(secondaryDetailText);
            _tertiaryDetailText.SetDynamicResource(StyleProperty, tertiaryDetailTextStyleResourceKey);
            _tertiaryDetailText.Text = tertiaryDetailText;
            _tertiaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(tertiaryDetailText);
            _progress.Progress = Progress;
            _progress.IsVisible = IsProgressVisible;

            Grid.SetColumn(_textStack, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_textStack, isLeadingIconVisible ? 1 : 2);
            Grid.SetColumn(_progress, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_progress, isLeadingIconVisible ? 1 : 2);

            SemanticProperties.SetDescription(
                this,
                CreateSemanticDescription(title, primaryDetailText, secondaryDetailText, tertiaryDetailText));
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey) ? defaultResourceKey : resourceKey;
        }

        private static string CreateSemanticDescription(
            string title,
            string primaryDetailText,
            string secondaryDetailText,
            string tertiaryDetailText)
        {
            List<string> parts = [title, primaryDetailText, secondaryDetailText, tertiaryDetailText];
            return string.Join(". ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
