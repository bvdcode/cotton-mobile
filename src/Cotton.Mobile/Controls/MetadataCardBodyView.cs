// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class MetadataCardBodyView : ContentView
    {
        private const string DefaultErrorTextStyleResourceKey = "M3ErrorSupportingWrap";
        private const string DefaultInlineGridStyleResourceKey = "M3InlineMetadataGrid";
        private const string DefaultLeadingInlineTextStyleResourceKey = "M3CardSupportingStrongLine";
        private const string DefaultPrimaryTextStyleResourceKey = "M3CardSupportingWrap";
        private const string DefaultSecondaryTextStyleResourceKey = "M3CardMetaLine";
        private const string DefaultStackStyleResourceKey = "M3MetadataCardBodyStack";
        private const string DefaultTrailingInlineTextStyleResourceKey = "M3CardSupportingLine";
        private const string ErrorTextOpacityAnimationName = "M3MetadataCardErrorTextOpacity";
        private const string InlineMetadataOpacityAnimationName = "M3MetadataCardInlineMetadataOpacity";
        private const string PrimaryTextOpacityAnimationName = "M3MetadataCardPrimaryTextOpacity";
        private const string ProgressOpacityAnimationName = "M3MetadataCardProgressOpacity";
        private const string SecondaryTextOpacityAnimationName = "M3MetadataCardSecondaryTextOpacity";

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(MetadataCardBodyView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsProgressVisibleProperty = BindableProperty.Create(
            nameof(IsProgressVisible),
            typeof(bool),
            typeof(MetadataCardBodyView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingInlineTextProperty = BindableProperty.Create(
            nameof(LeadingInlineText),
            typeof(string),
            typeof(MetadataCardBodyView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingInlineTextProperty = BindableProperty.Create(
            nameof(TrailingInlineText),
            typeof(string),
            typeof(MetadataCardBodyView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsInlineMetadataVisibleProperty = BindableProperty.Create(
            nameof(IsInlineMetadataVisible),
            typeof(bool),
            typeof(MetadataCardBodyView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryTextProperty = BindableProperty.Create(
            nameof(PrimaryText),
            typeof(string),
            typeof(MetadataCardBodyView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPrimaryTextVisibleProperty = BindableProperty.Create(
            nameof(IsPrimaryTextVisible),
            typeof(bool),
            typeof(MetadataCardBodyView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryTextProperty = BindableProperty.Create(
            nameof(SecondaryText),
            typeof(string),
            typeof(MetadataCardBodyView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSecondaryTextVisibleProperty = BindableProperty.Create(
            nameof(IsSecondaryTextVisible),
            typeof(bool),
            typeof(MetadataCardBodyView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(
            nameof(ErrorText),
            typeof(string),
            typeof(MetadataCardBodyView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsErrorTextVisibleProperty = BindableProperty.Create(
            nameof(IsErrorTextVisible),
            typeof(bool),
            typeof(MetadataCardBodyView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty InlineGridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(InlineGridStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultInlineGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingInlineTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingInlineTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultLeadingInlineTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingInlineTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingInlineTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultTrailingInlineTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultPrimaryTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultSecondaryTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ErrorTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ErrorTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardBodyView),
            DefaultErrorTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _errorText;
        private readonly Grid _inlineGrid;
        private readonly Label _leadingInlineText;
        private readonly LinearProgressView _progress;
        private readonly Label _primaryText;
        private readonly Label _secondaryText;
        private readonly VerticalStackLayout _stack;
        private readonly Label _trailingInlineText;
        private bool _hasAppliedVisibilityState;

        public MetadataCardBodyView()
        {
            _progress = new LinearProgressView();

            _leadingInlineText = new Label();
            _trailingInlineText = new Label();

            _inlineGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                Children =
                {
                    _leadingInlineText,
                    _trailingInlineText,
                },
            };
            Grid.SetColumn(_trailingInlineText, 1);

            _primaryText = new Label();
            _secondaryText = new Label();
            _errorText = new Label();

            _stack = new VerticalStackLayout
            {
                Children =
                {
                    _progress,
                    _inlineGrid,
                    _primaryText,
                    _secondaryText,
                    _errorText,
                },
            };

            Content = _stack;
            UpdateVisualState();
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

        public string LeadingInlineText
        {
            get => (string)GetValue(LeadingInlineTextProperty);
            set => SetValue(LeadingInlineTextProperty, value);
        }

        public string TrailingInlineText
        {
            get => (string)GetValue(TrailingInlineTextProperty);
            set => SetValue(TrailingInlineTextProperty, value);
        }

        public bool IsInlineMetadataVisible
        {
            get => (bool)GetValue(IsInlineMetadataVisibleProperty);
            set => SetValue(IsInlineMetadataVisibleProperty, value);
        }

        public string PrimaryText
        {
            get => (string)GetValue(PrimaryTextProperty);
            set => SetValue(PrimaryTextProperty, value);
        }

        public bool IsPrimaryTextVisible
        {
            get => (bool)GetValue(IsPrimaryTextVisibleProperty);
            set => SetValue(IsPrimaryTextVisibleProperty, value);
        }

        public string SecondaryText
        {
            get => (string)GetValue(SecondaryTextProperty);
            set => SetValue(SecondaryTextProperty, value);
        }

        public bool IsSecondaryTextVisible
        {
            get => (bool)GetValue(IsSecondaryTextVisibleProperty);
            set => SetValue(IsSecondaryTextVisibleProperty, value);
        }

        public string ErrorText
        {
            get => (string)GetValue(ErrorTextProperty);
            set => SetValue(ErrorTextProperty, value);
        }

        public bool IsErrorTextVisible
        {
            get => (bool)GetValue(IsErrorTextVisibleProperty);
            set => SetValue(IsErrorTextVisibleProperty, value);
        }

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        public string InlineGridStyleResourceKey
        {
            get => (string)GetValue(InlineGridStyleResourceKeyProperty);
            set => SetValue(InlineGridStyleResourceKeyProperty, value);
        }

        public string LeadingInlineTextStyleResourceKey
        {
            get => (string)GetValue(LeadingInlineTextStyleResourceKeyProperty);
            set => SetValue(LeadingInlineTextStyleResourceKeyProperty, value);
        }

        public string TrailingInlineTextStyleResourceKey
        {
            get => (string)GetValue(TrailingInlineTextStyleResourceKeyProperty);
            set => SetValue(TrailingInlineTextStyleResourceKeyProperty, value);
        }

        public string PrimaryTextStyleResourceKey
        {
            get => (string)GetValue(PrimaryTextStyleResourceKeyProperty);
            set => SetValue(PrimaryTextStyleResourceKeyProperty, value);
        }

        public string SecondaryTextStyleResourceKey
        {
            get => (string)GetValue(SecondaryTextStyleResourceKeyProperty);
            set => SetValue(SecondaryTextStyleResourceKeyProperty, value);
        }

        public string ErrorTextStyleResourceKey
        {
            get => (string)GetValue(ErrorTextStyleResourceKeyProperty);
            set => SetValue(ErrorTextStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MetadataCardBodyView view = (MetadataCardBodyView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            bool shouldAnimateVisibility = _hasAppliedVisibilityState;
            string stackStyleResourceKey = string.IsNullOrWhiteSpace(StackStyleResourceKey)
                ? DefaultStackStyleResourceKey
                : StackStyleResourceKey;
            string inlineGridStyleResourceKey = string.IsNullOrWhiteSpace(InlineGridStyleResourceKey)
                ? DefaultInlineGridStyleResourceKey
                : InlineGridStyleResourceKey;
            string leadingInlineTextStyleResourceKey = string.IsNullOrWhiteSpace(LeadingInlineTextStyleResourceKey)
                ? DefaultLeadingInlineTextStyleResourceKey
                : LeadingInlineTextStyleResourceKey;
            string trailingInlineTextStyleResourceKey = string.IsNullOrWhiteSpace(TrailingInlineTextStyleResourceKey)
                ? DefaultTrailingInlineTextStyleResourceKey
                : TrailingInlineTextStyleResourceKey;
            string primaryTextStyleResourceKey = string.IsNullOrWhiteSpace(PrimaryTextStyleResourceKey)
                ? DefaultPrimaryTextStyleResourceKey
                : PrimaryTextStyleResourceKey;
            string secondaryTextStyleResourceKey = string.IsNullOrWhiteSpace(SecondaryTextStyleResourceKey)
                ? DefaultSecondaryTextStyleResourceKey
                : SecondaryTextStyleResourceKey;
            string errorTextStyleResourceKey = string.IsNullOrWhiteSpace(ErrorTextStyleResourceKey)
                ? DefaultErrorTextStyleResourceKey
                : ErrorTextStyleResourceKey;
            string leadingInlineText = LeadingInlineText ?? string.Empty;
            string trailingInlineText = TrailingInlineText ?? string.Empty;
            string primaryText = PrimaryText ?? string.Empty;
            string secondaryText = SecondaryText ?? string.Empty;
            string errorText = ErrorText ?? string.Empty;
            bool isInlineMetadataVisible = IsInlineMetadataVisible
                && (!string.IsNullOrWhiteSpace(leadingInlineText) || !string.IsNullOrWhiteSpace(trailingInlineText));
            bool isPrimaryTextVisible = IsPrimaryTextVisible && !string.IsNullOrWhiteSpace(primaryText);
            bool isSecondaryTextVisible = IsSecondaryTextVisible && !string.IsNullOrWhiteSpace(secondaryText);
            bool isErrorTextVisible = IsErrorTextVisible && !string.IsNullOrWhiteSpace(errorText);

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
            _inlineGrid.SetDynamicResource(StyleProperty, inlineGridStyleResourceKey);
            _leadingInlineText.SetDynamicResource(StyleProperty, leadingInlineTextStyleResourceKey);
            _trailingInlineText.SetDynamicResource(StyleProperty, trailingInlineTextStyleResourceKey);
            _primaryText.SetDynamicResource(StyleProperty, primaryTextStyleResourceKey);
            _secondaryText.SetDynamicResource(StyleProperty, secondaryTextStyleResourceKey);
            _errorText.SetDynamicResource(StyleProperty, errorTextStyleResourceKey);

            _progress.Progress = Progress;
            UpdateElementVisibility(_progress, IsProgressVisible, ProgressOpacityAnimationName, shouldAnimateVisibility);
            UpdateElementVisibility(
                _inlineGrid,
                isInlineMetadataVisible,
                InlineMetadataOpacityAnimationName,
                shouldAnimateVisibility);
            UpdateElementVisibility(_primaryText, isPrimaryTextVisible, PrimaryTextOpacityAnimationName, shouldAnimateVisibility);
            UpdateElementVisibility(
                _secondaryText,
                isSecondaryTextVisible,
                SecondaryTextOpacityAnimationName,
                shouldAnimateVisibility);
            UpdateElementVisibility(_errorText, isErrorTextVisible, ErrorTextOpacityAnimationName, shouldAnimateVisibility);

            _leadingInlineText.Text = leadingInlineText;
            _trailingInlineText.Text = trailingInlineText;
            _primaryText.Text = primaryText;
            _secondaryText.Text = secondaryText;
            _errorText.Text = errorText;
            _hasAppliedVisibilityState = true;
        }

        private static void UpdateElementVisibility(
            VisualElement element,
            bool isElementVisible,
            string opacityAnimationName,
            bool animateVisibility)
        {
            double targetOpacity = isElementVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isElementVisible)
            {
                element.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                element,
                element.Opacity,
                targetOpacity,
                duration,
                opacityAnimationName,
                animateVisibility,
                opacity => element.Opacity = opacity,
                () => CompleteElementVisibility(element, isElementVisible));
        }

        private static void CompleteElementVisibility(VisualElement element, bool isElementVisible)
        {
            if (isElementVisible)
            {
                element.IsVisible = true;
                return;
            }

            element.IsVisible = false;
        }
    }
}
