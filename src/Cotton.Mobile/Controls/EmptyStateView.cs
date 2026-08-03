// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class EmptyStateView : ContentView
    {
        public static readonly BindableProperty IconDataProperty = CreateGeometryProperty(nameof(IconData));
        public static readonly BindableProperty ActionIconDataProperty = CreateGeometryProperty(nameof(ActionIconData));
        public static readonly BindableProperty TitleProperty = CreateTextProperty(nameof(Title));
        public static readonly BindableProperty BodyProperty = CreateTextProperty(nameof(Body));
        public static readonly BindableProperty ActionSemanticDescriptionProperty =
            CreateTextProperty(nameof(ActionSemanticDescription));

        public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(EmptyStateView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(EmptyStateView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStateVisibleProperty = BindableProperty.Create(
            nameof(IsStateVisible),
            typeof(bool),
            typeof(EmptyStateView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly Label _body;
        private readonly Border _card;
        private readonly IconView _icon;
        private readonly Label _title;

        public EmptyStateView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3EmptyStateIcon");

            Border iconFrame = new() { Content = _icon };
            iconFrame.SetDynamicResource(StyleProperty, "M3EmptyStateIconFrame");

            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3EmptyTitle");

            _body = new Label();
            _body.SetDynamicResource(StyleProperty, "M3EmptyBody");

            _actionButton = new IconButton();
            _actionButton.SetDynamicResource(StyleProperty, "M3EmptyStateActionIconButton");

            VerticalStackLayout stack = new()
            {
                Children =
                {
                    iconFrame,
                    _title,
                    _body,
                    _actionButton,
                },
            };
            stack.SetDynamicResource(StyleProperty, "M3EmptyStateStack");

            _card = new Border { Content = stack };
            _card.SetDynamicResource(StyleProperty, "M3EmptyStateSurface");
            Content = _card;

            UpdateVisualState();
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Body
        {
            get => (string)GetValue(BodyProperty);
            set => SetValue(BodyProperty, value);
        }

        public Geometry? ActionIconData
        {
            get => (Geometry?)GetValue(ActionIconDataProperty);
            set => SetValue(ActionIconDataProperty, value);
        }

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        public bool IsStateVisible
        {
            get => (bool)GetValue(IsStateVisibleProperty);
            set => SetValue(IsStateVisibleProperty, value);
        }

        private static BindableProperty CreateTextProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(EmptyStateView),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static BindableProperty CreateGeometryProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(Geometry),
                typeof(EmptyStateView),
                default(Geometry),
                propertyChanged: OnVisualPropertyChanged);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((EmptyStateView)bindable).UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_card is null)
            {
                return;
            }

            string title = Title ?? string.Empty;
            string body = Body ?? string.Empty;
            bool hasAction = IsActionVisible && ActionCommand is not null && ActionIconData is not null;

            IsVisible = IsStateVisible;
            _icon.IconData = IconData;
            _title.Text = title;
            _body.Text = body;
            _body.IsVisible = !string.IsNullOrWhiteSpace(body);

            _actionButton.IconData = ActionIconData;
            _actionButton.Command = ActionCommand;
            _actionButton.IsVisible = hasAction;
            SemanticProperties.SetDescription(_actionButton, ActionSemanticDescription ?? string.Empty);
            SemanticProperties.SetDescription(
                this,
                string.Join(", ", new[] { title, body }
                    .Where(value => !string.IsNullOrWhiteSpace(value))));
        }
    }
}
