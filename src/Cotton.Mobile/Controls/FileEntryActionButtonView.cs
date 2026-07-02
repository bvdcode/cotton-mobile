// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FileEntryActionButtonView : ContentView
    {
        private const string DefaultIconButtonStyleResourceKey = "M3FileChromeIconButton";
        private const string ActionButtonOpacityAnimationName = "M3FileEntryActionButtonOpacity";

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(FileEntryActionButtonView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(FileEntryActionButtonView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(FileEntryActionButtonView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(FileEntryActionButtonView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(FileEntryActionButtonView),
            true,
            propertyChanged: OnActionVisibilityPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(FileEntryActionButtonView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconButtonStyleResourceKey),
            typeof(string),
            typeof(FileEntryActionButtonView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private bool _hasAppliedActionVisibility;

        public FileEntryActionButtonView()
        {
            _actionButton = new IconButton();
            Content = _actionButton;
            UpdateVisualState(animateActionVisibility: false);
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

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileEntryActionButtonView view = (FileEntryActionButtonView)bindable;
            view.UpdateVisualState(animateActionVisibility: false);
        }

        private static void OnActionVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileEntryActionButtonView view = (FileEntryActionButtonView)bindable;
            view.UpdateVisualState(animateActionVisibility: true);
        }

        private void UpdateVisualState(bool animateActionVisibility)
        {
            string iconButtonStyleResourceKey = string.IsNullOrWhiteSpace(IconButtonStyleResourceKey)
                ? DefaultIconButtonStyleResourceKey
                : IconButtonStyleResourceKey;

            _actionButton.SetDynamicResource(StyleProperty, iconButtonStyleResourceKey);
            _actionButton.IconData = IconData ?? IconPathData.MoreVertical;
            _actionButton.Command = Command;
            _actionButton.CommandParameter = CommandParameter;
            _actionButton.IsEnabled = IsActionEnabled;
            UpdateActionButtonVisibility(animateActionVisibility);
            SemanticProperties.SetDescription(_actionButton, SemanticDescription ?? string.Empty);
        }

        private void UpdateActionButtonVisibility(bool animateActionVisibility)
        {
            bool shouldAnimate = animateActionVisibility && _hasAppliedActionVisibility;
            double targetOpacity = IsActionVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (IsActionVisible)
            {
                _actionButton.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _actionButton,
                _actionButton.Opacity,
                targetOpacity,
                duration,
                ActionButtonOpacityAnimationName,
                shouldAnimate,
                opacity => _actionButton.Opacity = opacity,
                CompleteActionButtonVisibility);
            _hasAppliedActionVisibility = true;
        }

        private void CompleteActionButtonVisibility()
        {
            if (IsActionVisible)
            {
                _actionButton.IsVisible = true;
                return;
            }

            _actionButton.IsVisible = false;
        }
    }
}
