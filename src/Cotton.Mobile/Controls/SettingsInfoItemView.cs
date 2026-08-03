// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsInfoItemView : ContentView
    {
        private const string AttentionLeadingIconStyle = "M3CardErrorThumbnailFrame";
        private const string AttentionTrailingTextStyle = "M3ErrorChipLabel";
        private const string DefaultLeadingIconStyle = "M3CardUtilityThumbnailFrame";
        private const string DefaultTitleStyle = "M3CardSupportingStrongLine";
        private const string DetailTextStyle = "M3CardSupportingLine";
        private const string GridStyle = "M3SettingsListItemGrid";
        private const string TextStackStyle = "M3CardTextStack";
        private const string TrailingChipStyle = "M3TrailingChip";
        private const string TrailingTextStyle = "M3ChipLabel";

        public static readonly BindableProperty TitleProperty = CreateTextProperty(nameof(Title));
        public static readonly BindableProperty PrimaryDetailTextProperty =
            CreateTextProperty(nameof(PrimaryDetailText));
        public static readonly BindableProperty SecondaryDetailTextProperty =
            CreateTextProperty(nameof(SecondaryDetailText));
        public static readonly BindableProperty TrailingTextProperty = CreateTextProperty(nameof(TrailingText));

        public static readonly BindableProperty LeadingIconDataProperty = CreateGeometryProperty(nameof(LeadingIconData));
        public static readonly BindableProperty AttentionLeadingIconDataProperty =
            CreateGeometryProperty(nameof(AttentionLeadingIconData));

        public static readonly BindableProperty IsAttentionStateProperty = BindableProperty.Create(
            nameof(IsAttentionState),
            typeof(bool),
            typeof(SettingsInfoItemView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = CreateStyleProperty(
            nameof(LeadingIconFrameStyleResourceKey),
            DefaultLeadingIconStyle);

        public static readonly BindableProperty TitleTextStyleResourceKeyProperty = CreateStyleProperty(
            nameof(TitleTextStyleResourceKey),
            DefaultTitleStyle);

        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly Label _primaryDetail;
        private readonly Label _secondaryDetail;
        private readonly Label _title;
        private readonly Border _trailingChip;
        private readonly Label _trailingText;
        private readonly VerticalStackLayout _textStack;

        public SettingsInfoItemView()
        {
            _leadingIcon = new IconFrame();
            _title = new Label();
            _primaryDetail = StyledLabel(DetailTextStyle);
            _secondaryDetail = StyledLabel(DetailTextStyle);
            _trailingText = new Label();
            _trailingChip = new Border { Content = _trailingText };
            _trailingChip.SetDynamicResource(StyleProperty, TrailingChipStyle);

            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _title,
                    _primaryDetail,
                    _secondaryDetail,
                },
            };
            _textStack.SetDynamicResource(StyleProperty, TextStackStyle);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                    _trailingChip,
                },
            };
            _grid.SetDynamicResource(StyleProperty, GridStyle);
            Grid.SetColumn(_textStack, 1);
            Grid.SetColumn(_trailingChip, 2);

            Content = _grid;
            InputTransparent = true;
            UpdateVisualState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string PrimaryDetailText
        {
            get => (string)GetValue(PrimaryDetailTextProperty);
            set => SetValue(PrimaryDetailTextProperty, value);
        }

        public string SecondaryDetailText
        {
            get => (string)GetValue(SecondaryDetailTextProperty);
            set => SetValue(SecondaryDetailTextProperty, value);
        }

        public string TrailingText
        {
            get => (string)GetValue(TrailingTextProperty);
            set => SetValue(TrailingTextProperty, value);
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public Geometry? AttentionLeadingIconData
        {
            get => (Geometry?)GetValue(AttentionLeadingIconDataProperty);
            set => SetValue(AttentionLeadingIconDataProperty, value);
        }

        public bool IsAttentionState
        {
            get => (bool)GetValue(IsAttentionStateProperty);
            set => SetValue(IsAttentionStateProperty, value);
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

        private static BindableProperty CreateTextProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(SettingsInfoItemView),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static BindableProperty CreateGeometryProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(Geometry),
                typeof(SettingsInfoItemView),
                default(Geometry),
                propertyChanged: OnVisualPropertyChanged);
        }

        private static BindableProperty CreateStyleProperty(string name, string defaultValue)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(SettingsInfoItemView),
                defaultValue,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static Label StyledLabel(string styleResourceKey)
        {
            var label = new Label();
            label.SetDynamicResource(StyleProperty, styleResourceKey);
            return label;
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((SettingsInfoItemView)bindable).UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_grid is null)
            {
                return;
            }

            string title = Title ?? string.Empty;
            string primaryDetail = PrimaryDetailText ?? string.Empty;
            string secondaryDetail = SecondaryDetailText ?? string.Empty;
            string trailingText = TrailingText ?? string.Empty;
            Geometry? iconData = IsAttentionState && AttentionLeadingIconData is not null
                ? AttentionLeadingIconData
                : LeadingIconData;
            bool hasLeadingIcon = iconData is not null;
            bool hasTrailingText = !string.IsNullOrWhiteSpace(trailingText);

            _title.Text = title;
            _title.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(TitleTextStyleResourceKey, DefaultTitleStyle));
            SetOptionalText(_primaryDetail, primaryDetail);
            SetOptionalText(_secondaryDetail, secondaryDetail);

            _leadingIcon.IconData = iconData;
            _leadingIcon.IsVisible = hasLeadingIcon;
            _leadingIcon.SetDynamicResource(
                StyleProperty,
                IsAttentionState
                    ? AttentionLeadingIconStyle
                    : MaterialResources.ResolveStyleResourceKey(
                        LeadingIconFrameStyleResourceKey,
                        DefaultLeadingIconStyle));

            _trailingText.Text = trailingText;
            _trailingText.SetDynamicResource(
                StyleProperty,
                IsAttentionState ? AttentionTrailingTextStyle : TrailingTextStyle);
            _trailingChip.IsVisible = hasTrailingText;

            int textColumn = hasLeadingIcon ? 1 : 0;
            Grid.SetColumn(_textStack, textColumn);
            Grid.SetColumnSpan(_textStack, 3 - textColumn - (hasTrailingText ? 1 : 0));

            SemanticProperties.SetDescription(
                this,
                string.Join(", ", new[] { title, primaryDetail, secondaryDetail, trailingText }
                    .Where(value => !string.IsNullOrWhiteSpace(value))));
        }

        private static void SetOptionalText(Label label, string text)
        {
            label.Text = text;
            label.IsVisible = !string.IsNullOrWhiteSpace(text);
        }
    }
}
