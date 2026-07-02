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

            _container.SetDynamicResource(StyleProperty, isError ? "M3FileErrorStatusPanel" : "M3FileStatusPanel");
            _grid.SetDynamicResource(StyleProperty, "M3FileStatusGrid");
            _icon.SetDynamicResource(StyleProperty, isError ? "M3FileErrorStatusIcon" : "M3FileStatusIcon");
            _text.SetDynamicResource(StyleProperty, isError ? "M3FileErrorStatusPrimaryText" : "M3FileStatusPrimaryText");
            _details.SetDynamicResource(StyleProperty, "M3FileStatusMetaText");

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
