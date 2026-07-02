// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class StorageBucketItemView : ContentView
    {
        private const string DefaultDetailTextStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultGridStyleResourceKey = "M3SettingsListItemGrid";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardFileThumbnailFrame";
        private const string DefaultMetricTextStyleResourceKey = "M3CardSupportingStrongLine";
        private const string DefaultTitleTextStyleResourceKey = "M3CardSupportingStrongLine";
        private const string PrimaryMetricTextOpacityAnimationName = "M3StorageBucketPrimaryMetricOpacity";
        private const string SecondaryMetricTextOpacityAnimationName = "M3StorageBucketSecondaryMetricOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(StorageBucketItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(StorageBucketItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryMetricTextProperty = BindableProperty.Create(
            nameof(PrimaryMetricText),
            typeof(string),
            typeof(StorageBucketItemView),
            string.Empty,
            propertyChanged: OnPrimaryMetricTextVisibilityPropertyChanged);

        public static readonly BindableProperty SecondaryMetricTextProperty = BindableProperty.Create(
            nameof(SecondaryMetricText),
            typeof(string),
            typeof(StorageBucketItemView),
            string.Empty,
            propertyChanged: OnSecondaryMetricTextVisibilityPropertyChanged);

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(StorageBucketItemView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsProgressVisibleProperty = BindableProperty.Create(
            nameof(IsProgressVisible),
            typeof(bool),
            typeof(StorageBucketItemView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(StorageBucketItemView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(StorageBucketItemView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(StorageBucketItemView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleTextStyleResourceKey),
            typeof(string),
            typeof(StorageBucketItemView),
            DefaultTitleTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MetricTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(MetricTextStyleResourceKey),
            typeof(string),
            typeof(StorageBucketItemView),
            DefaultMetricTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailTextStyleResourceKey),
            typeof(string),
            typeof(StorageBucketItemView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _detailText;
        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly LinearProgressView _progress;
        private readonly Label _primaryMetricText;
        private readonly Label _secondaryMetricText;
        private readonly Label _title;
        private bool _hasAppliedPrimaryMetricTextVisibility;
        private bool _hasAppliedSecondaryMetricTextVisibility;

        public StorageBucketItemView()
        {
            InputTransparent = true;

            _leadingIcon = new IconFrame();
            _title = new Label();
            _primaryMetricText = new Label();
            _detailText = new Label();
            _secondaryMetricText = new Label();
            _progress = new LinearProgressView();

            Grid.SetRowSpan(_leadingIcon, 3);
            Grid.SetColumn(_title, 1);
            Grid.SetColumn(_primaryMetricText, 2);
            Grid.SetRow(_detailText, 1);
            Grid.SetColumn(_detailText, 1);
            Grid.SetRow(_secondaryMetricText, 1);
            Grid.SetColumn(_secondaryMetricText, 2);
            Grid.SetRow(_progress, 2);
            Grid.SetColumn(_progress, 1);
            Grid.SetColumnSpan(_progress, 2);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _leadingIcon,
                    _title,
                    _primaryMetricText,
                    _detailText,
                    _secondaryMetricText,
                    _progress,
                },
            };

            Content = _grid;
            UpdateVisualState(
                animatePrimaryMetricTextVisibility: false,
                animateSecondaryMetricTextVisibility: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public string PrimaryMetricText
        {
            get => (string)GetValue(PrimaryMetricTextProperty);
            set => SetValue(PrimaryMetricTextProperty, value);
        }

        public string SecondaryMetricText
        {
            get => (string)GetValue(SecondaryMetricTextProperty);
            set => SetValue(SecondaryMetricTextProperty, value);
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public bool IsProgressVisible
        {
            get => (bool)GetValue(IsProgressVisibleProperty);
            set => SetValue(IsProgressVisibleProperty, value);
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
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

        public string TitleTextStyleResourceKey
        {
            get => (string)GetValue(TitleTextStyleResourceKeyProperty);
            set => SetValue(TitleTextStyleResourceKeyProperty, value);
        }

        public string MetricTextStyleResourceKey
        {
            get => (string)GetValue(MetricTextStyleResourceKeyProperty);
            set => SetValue(MetricTextStyleResourceKeyProperty, value);
        }

        public string DetailTextStyleResourceKey
        {
            get => (string)GetValue(DetailTextStyleResourceKeyProperty);
            set => SetValue(DetailTextStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            StorageBucketItemView view = (StorageBucketItemView)bindable;
            view.UpdateVisualState(
                animatePrimaryMetricTextVisibility: false,
                animateSecondaryMetricTextVisibility: false);
        }

        private static void OnPrimaryMetricTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            StorageBucketItemView view = (StorageBucketItemView)bindable;
            view.UpdateVisualState(
                animatePrimaryMetricTextVisibility: true,
                animateSecondaryMetricTextVisibility: false);
        }

        private static void OnSecondaryMetricTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            StorageBucketItemView view = (StorageBucketItemView)bindable;
            view.UpdateVisualState(
                animatePrimaryMetricTextVisibility: false,
                animateSecondaryMetricTextVisibility: true);
        }

        private void UpdateVisualState(
            bool animatePrimaryMetricTextVisibility,
            bool animateSecondaryMetricTextVisibility)
        {
            string title = Title ?? string.Empty;
            string detailText = DetailText ?? string.Empty;
            string primaryMetricText = PrimaryMetricText ?? string.Empty;
            string secondaryMetricText = SecondaryMetricText ?? string.Empty;
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string leadingIconFrameStyleResourceKey =
                ResolveStyleResourceKey(LeadingIconFrameStyleResourceKey, DefaultLeadingIconFrameStyleResourceKey);
            string titleTextStyleResourceKey =
                ResolveStyleResourceKey(TitleTextStyleResourceKey, DefaultTitleTextStyleResourceKey);
            string metricTextStyleResourceKey =
                ResolveStyleResourceKey(MetricTextStyleResourceKey, DefaultMetricTextStyleResourceKey);
            string detailTextStyleResourceKey =
                ResolveStyleResourceKey(DetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _leadingIcon.IconData = LeadingIconData;
            _title.SetDynamicResource(StyleProperty, titleTextStyleResourceKey);
            _title.Text = title;
            _primaryMetricText.SetDynamicResource(StyleProperty, metricTextStyleResourceKey);
            _primaryMetricText.Text = primaryMetricText;
            UpdateMetricTextVisibility(
                _primaryMetricText,
                primaryMetricText,
                animatePrimaryMetricTextVisibility,
                ref _hasAppliedPrimaryMetricTextVisibility,
                PrimaryMetricTextOpacityAnimationName,
                CompletePrimaryMetricTextVisibility);
            _detailText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _detailText.Text = detailText;
            _secondaryMetricText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _secondaryMetricText.Text = secondaryMetricText;
            UpdateMetricTextVisibility(
                _secondaryMetricText,
                secondaryMetricText,
                animateSecondaryMetricTextVisibility,
                ref _hasAppliedSecondaryMetricTextVisibility,
                SecondaryMetricTextOpacityAnimationName,
                CompleteSecondaryMetricTextVisibility);
            _progress.Progress = Progress;
            _progress.IsVisible = IsProgressVisible;
            SemanticProperties.SetDescription(
                this,
                CreateSemanticDescription(title, primaryMetricText, detailText, secondaryMetricText));
        }

        private void UpdateMetricTextVisibility(
            Label metricTextLabel,
            string metricText,
            bool animateMetricTextVisibility,
            ref bool hasAppliedMetricTextVisibility,
            string animationName,
            Action completeVisibility)
        {
            bool isMetricTextVisible = IsMetricTextActuallyVisible(metricText);
            bool shouldAnimate = animateMetricTextVisibility && hasAppliedMetricTextVisibility;
            double targetOpacity = isMetricTextVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isMetricTextVisible)
            {
                metricTextLabel.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                metricTextLabel,
                metricTextLabel.Opacity,
                targetOpacity,
                duration,
                animationName,
                shouldAnimate,
                opacity => metricTextLabel.Opacity = opacity,
                completeVisibility);
            hasAppliedMetricTextVisibility = true;
        }

        private void CompletePrimaryMetricTextVisibility()
        {
            if (IsMetricTextActuallyVisible(PrimaryMetricText ?? string.Empty))
            {
                _primaryMetricText.IsVisible = true;
                return;
            }

            _primaryMetricText.IsVisible = false;
        }

        private void CompleteSecondaryMetricTextVisibility()
        {
            if (IsMetricTextActuallyVisible(SecondaryMetricText ?? string.Empty))
            {
                _secondaryMetricText.IsVisible = true;
                return;
            }

            _secondaryMetricText.IsVisible = false;
        }

        private static bool IsMetricTextActuallyVisible(string metricText)
        {
            return !string.IsNullOrWhiteSpace(metricText);
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey) ? defaultResourceKey : resourceKey;
        }

        private static string CreateSemanticDescription(
            string title,
            string primaryMetricText,
            string detailText,
            string secondaryMetricText)
        {
            List<string> parts = [title, primaryMetricText, detailText, secondaryMetricText];
            return string.Join(". ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
