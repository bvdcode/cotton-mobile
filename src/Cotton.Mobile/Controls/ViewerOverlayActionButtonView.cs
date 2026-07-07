// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ViewerOverlayActionButtonView : MaterialAnimatedContentView
    {
        private const string DefaultIconButtonStyleResourceKey = "M3ViewerOverlayActionIconButton";

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(ViewerOverlayActionButtonView),
            true,
            propertyChanged: OnActionVisiblePropertyChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(ViewerOverlayActionButtonView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(ViewerOverlayActionButtonView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(ViewerOverlayActionButtonView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(ViewerOverlayActionButtonView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconButtonStyleResourceKey),
            typeof(string),
            typeof(ViewerOverlayActionButtonView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _button;

        public ViewerOverlayActionButtonView()
        {
            _button = new IconButton();

            Content = _button;
            UpdateVisualState();
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
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

        public string IconButtonStyleResourceKey
        {
            get => (string)GetValue(IconButtonStyleResourceKeyProperty);
            set => SetValue(IconButtonStyleResourceKeyProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal))
            {
                UpdateVisualState();
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerOverlayActionButtonView view = (ViewerOverlayActionButtonView)bindable;
            view.UpdateVisualState();
        }

        private static void OnActionVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerOverlayActionButtonView view = (ViewerOverlayActionButtonView)bindable;
            view.IsContentVisible = (bool)newValue;
        }

        private void UpdateVisualState()
        {
            string iconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IconButtonStyleResourceKey,
                DefaultIconButtonStyleResourceKey);

            _button.SetDynamicResource(StyleProperty, iconButtonStyleResourceKey);
            _button.IconData = IconData ?? IconPathData.Reset;
            _button.Command = Command;
            _button.CommandParameter = CommandParameter;
            _button.IsEnabled = IsEnabled;
            SemanticProperties.SetDescription(_button, SemanticDescription ?? string.Empty);
        }
    }
}
