// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class ViewerPlayOverlayView : ContentView
    {
        private const string DefaultContainerStyleResourceKey = "M3ViewerCenteredOverlay";
        private const string DefaultIconButtonStyleResourceKey = "M3ViewerCenteredPlayIconButton";

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(ViewerPlayOverlayView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(ViewerPlayOverlayView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(ViewerPlayOverlayView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ContainerStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ContainerStyleResourceKey),
            typeof(string),
            typeof(ViewerPlayOverlayView),
            DefaultContainerStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconButtonStyleResourceKey),
            typeof(string),
            typeof(ViewerPlayOverlayView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly VerticalStackLayout _container;
        private readonly IconButton _playButton;

        public ViewerPlayOverlayView()
        {
            _playButton = new IconButton
            {
                IconData = IconPathData.Play,
            };
            _container = new VerticalStackLayout
            {
                Children =
                {
                    _playButton,
                },
            };

            Content = _container;
            UpdateVisualState();
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public string SemanticDescription
        {
            get => (string)GetValue(SemanticDescriptionProperty);
            set => SetValue(SemanticDescriptionProperty, value);
        }

        public string ContainerStyleResourceKey
        {
            get => (string)GetValue(ContainerStyleResourceKeyProperty);
            set => SetValue(ContainerStyleResourceKeyProperty, value);
        }

        public string IconButtonStyleResourceKey
        {
            get => (string)GetValue(IconButtonStyleResourceKeyProperty);
            set => SetValue(IconButtonStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerPlayOverlayView view = (ViewerPlayOverlayView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string containerStyleResourceKey = string.IsNullOrWhiteSpace(ContainerStyleResourceKey)
                ? DefaultContainerStyleResourceKey
                : ContainerStyleResourceKey;
            string iconButtonStyleResourceKey = string.IsNullOrWhiteSpace(IconButtonStyleResourceKey)
                ? DefaultIconButtonStyleResourceKey
                : IconButtonStyleResourceKey;

            _container.SetDynamicResource(StyleProperty, containerStyleResourceKey);
            _playButton.SetDynamicResource(StyleProperty, iconButtonStyleResourceKey);
            _playButton.Command = Command;
            _playButton.CommandParameter = CommandParameter;
            SemanticProperties.SetDescription(_playButton, SemanticDescription ?? string.Empty);
        }
    }
}
