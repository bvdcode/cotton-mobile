// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public partial class NavigationBarItem
    {
        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(NavigationBarItem),
            false,
            propertyChanged: OnSelectionChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(NavigationBarItem),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurfaceVariant", "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(NavigationBarItem),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurfaceVariant", "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty PressedFillColorProperty = BindableProperty.Create(
            nameof(PressedFillColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerHigh", "M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty ContentSpacingProperty = BindableProperty.Create(
            nameof(ContentSpacing),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarContentSpacing"));

        public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
            nameof(ContentPadding),
            typeof(Thickness),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Thickness>("M3NavigationBarItemPadding"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarIconSize"));

        public static readonly BindableProperty ItemCornerRadiusProperty = BindableProperty.Create(
            nameof(ItemCornerRadius),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("ShapeExtraLarge"));

        public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
            nameof(ItemHeight),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarHeight"));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarLabelFontSize"));

        public static readonly BindableProperty TextFontAttributesProperty = BindableProperty.Create(
            nameof(TextFontAttributes),
            typeof(FontAttributes),
            typeof(NavigationBarItem),
            FontAttributes.None,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextFontFamilyProperty = BindableProperty.Create(
            nameof(TextFontFamily),
            typeof(string),
            typeof(NavigationBarItem),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(NavigationBarItem),
            propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged);

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public Color IconColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                IconColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
            set => SetValue(IconColorProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Color TextColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                TextColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
            set => SetValue(TextColorProperty, value);
        }

        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public Color PressedFillColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                PressedFillColorProperty,
                "M3LightSurfaceContainerHigh",
                "M3DarkSurfaceContainerHigh");
            set => SetValue(PressedFillColorProperty, value);
        }

        public Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }

        public double BorderWidth
        {
            get => (double)GetValue(BorderWidthProperty);
            set => SetValue(BorderWidthProperty, value);
        }

        public double ContentSpacing
        {
            get => (double)GetValue(ContentSpacingProperty);
            set => SetValue(ContentSpacingProperty, value);
        }

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double ItemCornerRadius
        {
            get => (double)GetValue(ItemCornerRadiusProperty);
            set => SetValue(ItemCornerRadiusProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public FontAttributes TextFontAttributes
        {
            get => (FontAttributes)GetValue(TextFontAttributesProperty);
            set => SetValue(TextFontAttributesProperty, value);
        }

        public string TextFontFamily
        {
            get => (string)GetValue(TextFontFamilyProperty);
            set => SetValue(TextFontFamilyProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
    }
}
