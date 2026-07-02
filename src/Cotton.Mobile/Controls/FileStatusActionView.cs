// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FileStatusActionView : ContentView
    {
        private const string DetailsOpacityAnimationName = "M3FileStatusDetailsOpacity";
        private const string DefaultPanelStyleResourceKey = "M3FileStatusPanel";
        private const string DefaultErrorPanelStyleResourceKey = "M3FileErrorStatusPanel";
        private const string DefaultGridStyleResourceKey = "M3FileStatusGrid";
        private const string DefaultIconStyleResourceKey = "M3FileStatusIcon";
        private const string DefaultErrorIconStyleResourceKey = "M3FileErrorStatusIcon";
        private const string DefaultTextStyleResourceKey = "M3FileStatusPrimaryText";
        private const string DefaultErrorTextStyleResourceKey = "M3FileErrorStatusPrimaryText";
        private const string DefaultDetailsStyleResourceKey = "M3FileStatusMetaText";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(FileStatusActionView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailsProperty = BindableProperty.Create(
            nameof(Details),
            typeof(string),
            typeof(FileStatusActionView),
            string.Empty,
            propertyChanged: OnDetailsVisibilityPropertyChanged);

        public static readonly BindableProperty AccessibilityTextProperty = BindableProperty.Create(
            nameof(AccessibilityText),
            typeof(string),
            typeof(FileStatusActionView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(FileStatusActionView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ErrorIconDataProperty = BindableProperty.Create(
            nameof(ErrorIconData),
            typeof(Geometry),
            typeof(FileStatusActionView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsErrorProperty = BindableProperty.Create(
            nameof(IsError),
            typeof(bool),
            typeof(FileStatusActionView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(FileStatusActionView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(FileStatusActionView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PanelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PanelStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultPanelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ErrorPanelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ErrorPanelStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultErrorPanelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultIconStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ErrorIconStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ErrorIconStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultErrorIconStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ErrorTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ErrorTextStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultErrorTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailsStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailsStyleResourceKey),
            typeof(string),
            typeof(FileStatusActionView),
            DefaultDetailsStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _container;
        private readonly Label _details;
        private readonly ColumnDefinition _detailsColumn;
        private readonly Grid _grid;
        private readonly IconView _icon;
        private readonly Label _text;
        private readonly TouchSurfaceView _touchSurface;
        private bool _hasAppliedDetailsVisibility;

        public FileStatusActionView()
        {
            _icon = new IconView();

            _text = new Label();
            Grid.SetColumn(_text, 1);

            _details = new Label();
            Grid.SetColumn(_details, 2);

            _touchSurface = new TouchSurfaceView();
            Grid.SetColumnSpan(_touchSurface, 3);

            _detailsColumn = new ColumnDefinition
            {
                Width = GridLength.Star,
            };

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Star,
                    },
                    _detailsColumn,
                },
                Children =
                {
                    _icon,
                    _text,
                    _details,
                    _touchSurface,
                },
            };

            _container = new Border
            {
                Content = _grid,
            };

            Content = _container;
            UpdateVisualState(animateDetailsVisibility: false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Details
        {
            get => (string)GetValue(DetailsProperty);
            set => SetValue(DetailsProperty, value);
        }

        public string AccessibilityText
        {
            get => (string)GetValue(AccessibilityTextProperty);
            set => SetValue(AccessibilityTextProperty, value);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public Geometry? ErrorIconData
        {
            get => (Geometry?)GetValue(ErrorIconDataProperty);
            set => SetValue(ErrorIconDataProperty, value);
        }

        public bool IsError
        {
            get => (bool)GetValue(IsErrorProperty);
            set => SetValue(IsErrorProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public string PanelStyleResourceKey
        {
            get => (string)GetValue(PanelStyleResourceKeyProperty);
            set => SetValue(PanelStyleResourceKeyProperty, value);
        }

        public string ErrorPanelStyleResourceKey
        {
            get => (string)GetValue(ErrorPanelStyleResourceKeyProperty);
            set => SetValue(ErrorPanelStyleResourceKeyProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string IconStyleResourceKey
        {
            get => (string)GetValue(IconStyleResourceKeyProperty);
            set => SetValue(IconStyleResourceKeyProperty, value);
        }

        public string ErrorIconStyleResourceKey
        {
            get => (string)GetValue(ErrorIconStyleResourceKeyProperty);
            set => SetValue(ErrorIconStyleResourceKeyProperty, value);
        }

        public string TextStyleResourceKey
        {
            get => (string)GetValue(TextStyleResourceKeyProperty);
            set => SetValue(TextStyleResourceKeyProperty, value);
        }

        public string ErrorTextStyleResourceKey
        {
            get => (string)GetValue(ErrorTextStyleResourceKeyProperty);
            set => SetValue(ErrorTextStyleResourceKeyProperty, value);
        }

        public string DetailsStyleResourceKey
        {
            get => (string)GetValue(DetailsStyleResourceKeyProperty);
            set => SetValue(DetailsStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileStatusActionView view = (FileStatusActionView)bindable;
            view.UpdateVisualState(animateDetailsVisibility: false);
        }

        private static void OnDetailsVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileStatusActionView view = (FileStatusActionView)bindable;
            view.UpdateVisualState(animateDetailsVisibility: true);
        }

        private void UpdateVisualState(bool animateDetailsVisibility)
        {
            string text = Text ?? string.Empty;
            string details = Details ?? string.Empty;
            string accessibilityText = string.IsNullOrWhiteSpace(AccessibilityText)
                ? CreateAccessibilityText(text, details)
                : AccessibilityText;
            ICommand? command = Command;
            bool isError = IsError;
            string panelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                isError ? ErrorPanelStyleResourceKey : PanelStyleResourceKey,
                isError ? DefaultErrorPanelStyleResourceKey : DefaultPanelStyleResourceKey);
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);
            string iconStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                isError ? ErrorIconStyleResourceKey : IconStyleResourceKey,
                isError ? DefaultErrorIconStyleResourceKey : DefaultIconStyleResourceKey);
            string textStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                isError ? ErrorTextStyleResourceKey : TextStyleResourceKey,
                isError ? DefaultErrorTextStyleResourceKey : DefaultTextStyleResourceKey);
            string detailsStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                DetailsStyleResourceKey,
                DefaultDetailsStyleResourceKey);

            _container.SetDynamicResource(StyleProperty, panelStyleResourceKey);
            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _icon.SetDynamicResource(StyleProperty, iconStyleResourceKey);
            _text.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _details.SetDynamicResource(StyleProperty, detailsStyleResourceKey);

            _icon.IconData = isError && ErrorIconData is not null
                ? ErrorIconData
                : IconData;
            _text.Text = text;
            _details.Text = details;
            UpdateDetailsVisibility(details, animateDetailsVisibility);
            _touchSurface.TapCommand = IsActionEnabled ? command : null;
            _touchSurface.IsVisible = IsActionEnabled && command is not null;
            SemanticProperties.SetDescription(_container, accessibilityText);
        }

        private void UpdateDetailsVisibility(string details, bool animateDetailsVisibility)
        {
            bool isDetailsVisible = IsDetailsVisible(details);
            bool shouldAnimate = animateDetailsVisibility && _hasAppliedDetailsVisibility;
            double targetOpacity = isDetailsVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isDetailsVisible)
            {
                _details.IsVisible = true;
                _detailsColumn.Width = GridLength.Star;
                Grid.SetColumnSpan(_text, 1);
            }

            MaterialMotion.UpdateDouble(
                _details,
                _details.Opacity,
                targetOpacity,
                duration,
                DetailsOpacityAnimationName,
                shouldAnimate,
                opacity => _details.Opacity = opacity,
                CompleteDetailsVisibility);
            _hasAppliedDetailsVisibility = true;
        }

        private void CompleteDetailsVisibility()
        {
            if (IsDetailsVisible(Details ?? string.Empty))
            {
                _details.IsVisible = true;
                _detailsColumn.Width = GridLength.Star;
                Grid.SetColumnSpan(_text, 1);
                return;
            }

            _details.IsVisible = false;
            _detailsColumn.Width = new GridLength(0);
            Grid.SetColumnSpan(_text, 2);
        }

        private bool IsDetailsVisible(string details)
        {
            return !string.IsNullOrWhiteSpace(details);
        }

        private string CreateAccessibilityText(string text, string details)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                return text;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return details;
            }

            return $"{text}. {details}";
        }
    }
}
