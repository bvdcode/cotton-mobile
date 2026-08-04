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
        private readonly Border _iconFrame;
        private readonly IconView _icon;
        private readonly Grid _layout;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _title;
        private bool? _isCompact;

        public EmptyStateView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3EmptyStateIcon");

            _iconFrame = new Border { Content = _icon };
            _iconFrame.SetDynamicResource(StyleProperty, "M3EmptyStateIconFrame");

            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3EmptyTitle");

            _body = new Label();
            _body.SetDynamicResource(StyleProperty, "M3EmptyBody");

            _actionButton = new IconButton();
            _actionButton.SetDynamicResource(StyleProperty, "M3EmptyStateActionIconButton");

            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _title,
                    _body,
                },
            };
            _textStack.SetDynamicResource(StyleProperty, "M3EmptyStateTextStack");

            _layout = new Grid
            {
                Children =
                {
                    _iconFrame,
                    _textStack,
                    _actionButton,
                },
            };

            _card = new Border { Content = _layout };
            Content = _card;

            UpdateLayout(false);
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

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            UpdateLayout(width > height);
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

        private void UpdateLayout(bool isCompact)
        {
            if (_isCompact == isCompact)
            {
                return;
            }

            _isCompact = isCompact;
            _layout.RowDefinitions.Clear();
            _layout.ColumnDefinitions.Clear();

            if (isCompact)
            {
                _layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                _layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                _layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                _layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetRow(_iconFrame, 0);
                Grid.SetColumn(_iconFrame, 0);
                Grid.SetRow(_textStack, 0);
                Grid.SetColumn(_textStack, 1);
                Grid.SetRow(_actionButton, 0);
                Grid.SetColumn(_actionButton, 2);

                _layout.SetDynamicResource(StyleProperty, "M3EmptyStateCompactLayout");
                _card.SetDynamicResource(StyleProperty, "M3EmptyStateCompactSurface");
                _title.SetDynamicResource(StyleProperty, "M3EmptyTitleCompact");
                _body.SetDynamicResource(StyleProperty, "M3EmptyBodyCompact");
                return;
            }

            _layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            Grid.SetRow(_iconFrame, 0);
            Grid.SetColumn(_iconFrame, 0);
            Grid.SetRow(_textStack, 1);
            Grid.SetColumn(_textStack, 0);
            Grid.SetRow(_actionButton, 2);
            Grid.SetColumn(_actionButton, 0);

            _layout.SetDynamicResource(StyleProperty, "M3EmptyStateLayout");
            _card.SetDynamicResource(StyleProperty, "M3EmptyStateSurface");
            _title.SetDynamicResource(StyleProperty, "M3EmptyTitle");
            _body.SetDynamicResource(StyleProperty, "M3EmptyBody");
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
