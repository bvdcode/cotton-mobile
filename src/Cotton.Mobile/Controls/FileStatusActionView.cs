// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FileStatusActionView : ContentView
    {
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
            propertyChanged: OnVisualPropertyChanged);

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
        private readonly Grid _grid;
        private readonly IconView _icon;
        private readonly Label _text;
        private readonly TouchSurfaceView _touchSurface;

        public FileStatusActionView()
        {
            _icon = new IconView();

            _text = new Label();
            Grid.SetColumn(_text, 1);

            _details = new Label();
            Grid.SetColumn(_details, 2);

            _touchSurface = new TouchSurfaceView();
            Grid.SetColumnSpan(_touchSurface, 3);

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
                    new ColumnDefinition
                    {
                        Width = GridLength.Star,
                    },
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
            UpdateVisualState();
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
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
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
            _touchSurface.TapCommand = IsActionEnabled ? command : null;
            _touchSurface.IsVisible = IsActionEnabled && command is not null;
            SemanticProperties.SetDescription(_container, accessibilityText);
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
