// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ActionClusterView : HorizontalStackLayout
    {
        private const string DefaultActionIconButtonStyleResourceKey = "M3FileChromeIconButton";

        public static readonly BindableProperty PrimaryActionIconDataProperty = BindableProperty.Create(
            nameof(PrimaryActionIconData),
            typeof(Geometry),
            typeof(ActionClusterView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionCommandProperty = BindableProperty.Create(
            nameof(PrimaryActionCommand),
            typeof(ICommand),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionCommandParameterProperty = BindableProperty.Create(
            nameof(PrimaryActionCommandParameter),
            typeof(object),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(PrimaryActionSemanticDescription),
            typeof(string),
            typeof(ActionClusterView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconDataProperty = BindableProperty.Create(
            nameof(SecondaryActionIconData),
            typeof(Geometry),
            typeof(ActionClusterView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionCommandProperty = BindableProperty.Create(
            nameof(SecondaryActionCommand),
            typeof(ICommand),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionCommandParameterProperty = BindableProperty.Create(
            nameof(SecondaryActionCommandParameter),
            typeof(object),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(SecondaryActionSemanticDescription),
            typeof(string),
            typeof(ActionClusterView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _primaryActionButton;
        private readonly IconButton _secondaryActionButton;

        public ActionClusterView()
        {
            _primaryActionButton = new IconButton();
            _secondaryActionButton = new IconButton();

            Children.Add(_primaryActionButton);
            Children.Add(_secondaryActionButton);
            UpdateVisualState();
        }

        public Geometry? PrimaryActionIconData
        {
            get => (Geometry?)GetValue(PrimaryActionIconDataProperty);
            set => SetValue(PrimaryActionIconDataProperty, value);
        }

        public ICommand? PrimaryActionCommand
        {
            get => (ICommand?)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }

        public object? PrimaryActionCommandParameter
        {
            get => GetValue(PrimaryActionCommandParameterProperty);
            set => SetValue(PrimaryActionCommandParameterProperty, value);
        }

        public string PrimaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(PrimaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(PrimaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string PrimaryActionSemanticDescription
        {
            get => (string)GetValue(PrimaryActionSemanticDescriptionProperty);
            set => SetValue(PrimaryActionSemanticDescriptionProperty, value);
        }

        public Geometry? SecondaryActionIconData
        {
            get => (Geometry?)GetValue(SecondaryActionIconDataProperty);
            set => SetValue(SecondaryActionIconDataProperty, value);
        }

        public ICommand? SecondaryActionCommand
        {
            get => (ICommand?)GetValue(SecondaryActionCommandProperty);
            set => SetValue(SecondaryActionCommandProperty, value);
        }

        public object? SecondaryActionCommandParameter
        {
            get => GetValue(SecondaryActionCommandParameterProperty);
            set => SetValue(SecondaryActionCommandParameterProperty, value);
        }

        public string SecondaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(SecondaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(SecondaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string SecondaryActionSemanticDescription
        {
            get => (string)GetValue(SecondaryActionSemanticDescriptionProperty);
            set => SetValue(SecondaryActionSemanticDescriptionProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ActionClusterView view = (ActionClusterView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            SetDynamicResource(StyleProperty, "M3RowActionCluster");

            UpdateActionButton(
                _primaryActionButton,
                PrimaryActionIconData,
                PrimaryActionCommand,
                PrimaryActionCommandParameter,
                PrimaryActionIconButtonStyleResourceKey,
                PrimaryActionSemanticDescription ?? string.Empty);
            UpdateActionButton(
                _secondaryActionButton,
                SecondaryActionIconData,
                SecondaryActionCommand,
                SecondaryActionCommandParameter,
                SecondaryActionIconButtonStyleResourceKey,
                SecondaryActionSemanticDescription ?? string.Empty);
        }

        private static void UpdateActionButton(
            IconButton actionButton,
            Geometry? iconData,
            ICommand? command,
            object? commandParameter,
            string iconButtonStyleResourceKey,
            string semanticDescription)
        {
            string styleResourceKey = string.IsNullOrWhiteSpace(iconButtonStyleResourceKey)
                ? DefaultActionIconButtonStyleResourceKey
                : iconButtonStyleResourceKey;

            actionButton.SetDynamicResource(StyleProperty, styleResourceKey);
            actionButton.IconData = iconData;
            actionButton.Command = command;
            actionButton.CommandParameter = commandParameter;
            actionButton.IsVisible = iconData is not null && command is not null;
            SemanticProperties.SetDescription(actionButton, semanticDescription);
        }
    }
}
