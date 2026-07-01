// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class EmptyStateView : ContentView
    {
        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(EmptyStateView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BodyProperty = BindableProperty.Create(
            nameof(Body),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _card;
        private readonly IconView _icon;
        private readonly Label _title;
        private readonly Label _body;

        public EmptyStateView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3EmptyStateIcon");

            Border iconFrame = new()
            {
                Content = _icon,
            };
            iconFrame.SetDynamicResource(StyleProperty, "M3EmptyStateIconFrame");

            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3EmptyTitle");

            _body = new Label();
            _body.SetDynamicResource(StyleProperty, "M3EmptyBody");

            VerticalStackLayout stack = new()
            {
                Children =
                {
                    iconFrame,
                    _title,
                    _body,
                },
            };
            stack.SetDynamicResource(StyleProperty, "M3EmptyStateStack");

            _card = new Border
            {
                Content = stack,
            };
            _card.SetDynamicResource(StyleProperty, "M3EmptyStateCard");

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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            EmptyStateView emptyStateView = (EmptyStateView)bindable;
            emptyStateView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string title = Title ?? string.Empty;
            string body = Body ?? string.Empty;

            _icon.IconData = IconData;
            _title.Text = title;
            _body.Text = body;
            _body.IsVisible = !string.IsNullOrWhiteSpace(body);

            string description = string.IsNullOrWhiteSpace(body)
                ? title
                : $"{title}. {body}";
            SemanticProperties.SetDescription(_card, description);
        }
    }
}
