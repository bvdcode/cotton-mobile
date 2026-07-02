// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FileEntryTextView : ContentView
    {
        private const string DefaultDetailStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultStackStyleResourceKey = "M3FileListTextStack";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";
        private const string DetailOpacityAnimationName = "M3FileEntryDetailOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FileEntryTextView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(FileEntryTextView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsDetailVisibleProperty = BindableProperty.Create(
            nameof(IsDetailVisible),
            typeof(bool),
            typeof(FileEntryTextView),
            true,
            propertyChanged: OnDetailVisiblePropertyChanged);

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(FileEntryTextView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(FileEntryTextView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailStyleResourceKey),
            typeof(string),
            typeof(FileEntryTextView),
            DefaultDetailStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _detailLabel;
        private readonly VerticalStackLayout _stack;
        private readonly Label _titleLabel;
        private bool _hasAppliedDetailVisibility;

        public FileEntryTextView()
        {
            InputTransparent = true;

            _titleLabel = new Label();
            _detailLabel = new Label();
            _stack = new VerticalStackLayout
            {
                Children =
                {
                    _titleLabel,
                    _detailLabel,
                },
            };

            Content = _stack;
            UpdateVisualState(animateDetailVisibility: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Detail
        {
            get => (string)GetValue(DetailProperty);
            set => SetValue(DetailProperty, value);
        }

        public bool IsDetailVisible
        {
            get => (bool)GetValue(IsDetailVisibleProperty);
            set => SetValue(IsDetailVisibleProperty, value);
        }

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string DetailStyleResourceKey
        {
            get => (string)GetValue(DetailStyleResourceKeyProperty);
            set => SetValue(DetailStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileEntryTextView view = (FileEntryTextView)bindable;
            view.UpdateVisualState(animateDetailVisibility: false);
        }

        private static void OnDetailVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileEntryTextView view = (FileEntryTextView)bindable;
            view.UpdateVisualState(animateDetailVisibility: true);
        }

        private void UpdateVisualState(bool animateDetailVisibility)
        {
            string stackStyleResourceKey = string.IsNullOrWhiteSpace(StackStyleResourceKey)
                ? DefaultStackStyleResourceKey
                : StackStyleResourceKey;
            string titleStyleResourceKey = string.IsNullOrWhiteSpace(TitleStyleResourceKey)
                ? DefaultTitleStyleResourceKey
                : TitleStyleResourceKey;
            string detailStyleResourceKey = string.IsNullOrWhiteSpace(DetailStyleResourceKey)
                ? DefaultDetailStyleResourceKey
                : DetailStyleResourceKey;

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
            _titleLabel.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _detailLabel.SetDynamicResource(StyleProperty, detailStyleResourceKey);

            _titleLabel.Text = Title ?? string.Empty;
            _detailLabel.Text = Detail ?? string.Empty;
            UpdateDetailVisibility(animateDetailVisibility);
        }

        private void UpdateDetailVisibility(bool animateDetailVisibility)
        {
            bool isDetailVisible = IsDetailVisible;
            bool shouldAnimate = animateDetailVisibility && _hasAppliedDetailVisibility;
            double targetOpacity = isDetailVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isDetailVisible)
            {
                _detailLabel.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _detailLabel,
                _detailLabel.Opacity,
                targetOpacity,
                duration,
                DetailOpacityAnimationName,
                shouldAnimate,
                opacity => _detailLabel.Opacity = opacity,
                CompleteDetailVisibility);
            _hasAppliedDetailVisibility = true;
        }

        private void CompleteDetailVisibility()
        {
            if (IsDetailVisible)
            {
                _detailLabel.IsVisible = true;
                return;
            }

            _detailLabel.IsVisible = false;
        }
    }
}
