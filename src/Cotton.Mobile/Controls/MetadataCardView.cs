// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(BodyContent))]
    public class MetadataCardView : ContentView
    {
        private const string BodyContentOpacityAnimationName = "M3MetadataCardBodyContentOpacity";
        private const string DefaultCardStyleResourceKey = "M3ContentCard";
        private const string DefaultGridStyleResourceKey = "M3MetadataCardGrid";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardFileThumbnailFrame";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(MetadataCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
            typeof(string),
            typeof(MetadataCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(MetadataCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsTrailingTextVisibleProperty = BindableProperty.Create(
            nameof(IsTrailingTextVisible),
            typeof(bool),
            typeof(MetadataCardView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(MetadataCardView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(MetadataCardView),
            DefaultCardStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(MetadataCardView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(MetadataCardView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(
            nameof(BodyContent),
            typeof(View),
            typeof(MetadataCardView),
            default(View),
            propertyChanged: OnBodyContentVisibilityPropertyChanged);

        private readonly ContentView _bodyContentHost;
        private readonly Border _card;
        private readonly Grid _grid;
        private readonly MetadataCardHeaderView _header;
        private bool _hasAppliedBodyContentVisibility;

        public MetadataCardView()
        {
            _header = new MetadataCardHeaderView();
            Grid.SetColumnSpan(_header, 3);

            _bodyContentHost = new ContentView();
            Grid.SetRow(_bodyContentHost, 1);
            Grid.SetColumn(_bodyContentHost, 1);
            Grid.SetColumnSpan(_bodyContentHost, 2);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(MaterialResources.Get<double>("M3FileThumbnailSize")) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _header,
                    _bodyContentHost,
                },
            };

            _card = new Border
            {
                Content = _grid,
            };

            Content = _card;
            UpdateVisualState(animateBodyContentVisibility: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string SupportingText
        {
            get => (string)GetValue(SupportingTextProperty);
            set => SetValue(SupportingTextProperty, value);
        }

        public string TrailingText
        {
            get => (string)GetValue(TrailingTextProperty);
            set => SetValue(TrailingTextProperty, value);
        }

        public bool IsTrailingTextVisible
        {
            get => (bool)GetValue(IsTrailingTextVisibleProperty);
            set => SetValue(IsTrailingTextVisibleProperty, value);
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
        }

        public View? BodyContent
        {
            get => (View?)GetValue(BodyContentProperty);
            set => SetValue(BodyContentProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MetadataCardView view = (MetadataCardView)bindable;
            view.UpdateVisualState(animateBodyContentVisibility: false);
        }

        private static void OnBodyContentVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            MetadataCardView view = (MetadataCardView)bindable;
            view.UpdateVisualState(animateBodyContentVisibility: true);
        }

        private void UpdateVisualState(bool animateBodyContentVisibility)
        {
            string cardStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                CardStyleResourceKey,
                DefaultCardStyleResourceKey);
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);
            string leadingIconFrameStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                LeadingIconFrameStyleResourceKey,
                DefaultLeadingIconFrameStyleResourceKey);
            View? bodyContent = BodyContent;

            _card.SetDynamicResource(StyleProperty, cardStyleResourceKey);
            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _header.ApplyHeaderState(
                Title ?? string.Empty,
                SupportingText ?? string.Empty,
                TrailingText ?? string.Empty,
                IsTrailingTextVisible,
                LeadingIconData,
                leadingIconFrameStyleResourceKey);

            bool hasBodyContent = bodyContent is not null;
            if (hasBodyContent && _bodyContentHost.Content != bodyContent)
            {
                _bodyContentHost.Content = bodyContent;
            }

            UpdateBodyContentVisibility(hasBodyContent, animateBodyContentVisibility);
        }

        private void UpdateBodyContentVisibility(bool hasBodyContent, bool animateBodyContentVisibility)
        {
            bool shouldAnimate = animateBodyContentVisibility && _hasAppliedBodyContentVisibility;
            double targetOpacity = hasBodyContent
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (hasBodyContent)
            {
                _bodyContentHost.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _bodyContentHost,
                _bodyContentHost.Opacity,
                targetOpacity,
                duration,
                BodyContentOpacityAnimationName,
                shouldAnimate,
                opacity => _bodyContentHost.Opacity = opacity,
                CompleteBodyContentVisibility);
            _hasAppliedBodyContentVisibility = true;
        }

        private void CompleteBodyContentVisibility()
        {
            View? bodyContent = BodyContent;
            if (bodyContent is not null)
            {
                if (_bodyContentHost.Content != bodyContent)
                {
                    _bodyContentHost.Content = bodyContent;
                }

                _bodyContentHost.IsVisible = true;
                return;
            }

            _bodyContentHost.Content = null;
            _bodyContentHost.IsVisible = false;
        }
    }
}
