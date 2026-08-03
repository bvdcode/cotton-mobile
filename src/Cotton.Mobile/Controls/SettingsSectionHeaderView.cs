// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsSectionHeaderView : ContentView
    {
        private const string DetailTextStyle = "M3CardSupportingBlock";
        private const string GridStyle = "M3SettingsListItemGrid";
        private const string TextStackStyle = "M3CardTextStack";
        private const string TitleStyle = "M3CardTitle";
        private const string DefaultLeadingIconStyle = "M3CardUtilityThumbnailFrame";

        public static readonly BindableProperty TitleProperty = CreateTextProperty(nameof(Title));
        public static readonly BindableProperty PrimaryDetailTextProperty =
            CreateTextProperty(nameof(PrimaryDetailText));
        public static readonly BindableProperty SecondaryDetailTextProperty =
            CreateTextProperty(nameof(SecondaryDetailText));

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsSectionHeaderView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultLeadingIconStyle,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly Label _primaryDetail;
        private readonly Label _secondaryDetail;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _title;

        public SettingsSectionHeaderView()
        {
            _leadingIcon = new IconFrame();
            _title = StyledLabel(TitleStyle);
            _primaryDetail = StyledLabel(DetailTextStyle);
            _secondaryDetail = StyledLabel(DetailTextStyle);

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
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                },
            };
            _grid.SetDynamicResource(StyleProperty, GridStyle);
            Grid.SetColumn(_textStack, 1);

            Content = _grid;
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

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
        }

        private static BindableProperty CreateTextProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(SettingsSectionHeaderView),
                string.Empty,
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
            ((SettingsSectionHeaderView)bindable).UpdateVisualState();
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
            bool hasLeadingIcon = LeadingIconData is not null;

            _title.Text = title;
            SetOptionalText(_primaryDetail, primaryDetail);
            SetOptionalText(_secondaryDetail, secondaryDetail);

            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = hasLeadingIcon;
            _leadingIcon.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(
                    LeadingIconFrameStyleResourceKey,
                    DefaultLeadingIconStyle));

            Grid.SetColumn(_textStack, hasLeadingIcon ? 1 : 0);
            Grid.SetColumnSpan(_textStack, hasLeadingIcon ? 1 : 2);

            SemanticProperties.SetDescription(
                this,
                string.Join(", ", new[] { title, primaryDetail, secondaryDetail }
                    .Where(value => !string.IsNullOrWhiteSpace(value))));
        }

        private static void SetOptionalText(Label label, string text)
        {
            label.Text = text;
            label.IsVisible = !string.IsNullOrWhiteSpace(text);
        }
    }
}
