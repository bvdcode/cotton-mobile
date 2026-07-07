// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class ScreenScrollBodyView : MaterialAnimatedContentView
    {
        private const string DefaultStackStyleResourceKey = "M3ScreenContentStack";

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(ScreenScrollBodyView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ScrollView _scrollView;
        private readonly VerticalStackLayout _stack;

        public ScreenScrollBodyView()
        {
            _stack = new VerticalStackLayout();
            _scrollView = new ScrollView
            {
                Content = _stack,
            };

            Content = _scrollView;
            UpdateVisualState();
        }

        public IList<IView> Items => _stack.Children;

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenScrollBodyView view = (ScreenScrollBodyView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string stackStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                StackStyleResourceKey,
                DefaultStackStyleResourceKey);

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
        }
    }
}
