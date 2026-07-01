// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ActionSheetItemView : CommandPressableContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ActionSheetItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(ActionSheetItemView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(ActionSheetItemView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkOnSurface"));

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Accent"));

        public static readonly BindableProperty RowBackgroundColorProperty = BindableProperty.Create(
            nameof(RowBackgroundColor),
            typeof(Color),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkSurfaceContainerLow"));

        public static readonly BindableProperty PressedRowBackgroundColorProperty = BindableProperty.Create(
            nameof(PressedRowBackgroundColor),
            typeof(Color),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty IconFrameBackgroundColorProperty = BindableProperty.Create(
            nameof(IconFrameBackgroundColor),
            typeof(Color),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkSurfaceContainer"));

        public static readonly BindableProperty IconFrameBorderColorProperty = BindableProperty.Create(
            nameof(IconFrameBorderColor),
            typeof(Color),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkOutlineVariant"));

        public static readonly BindableProperty RowCornerRadiusProperty = BindableProperty.Create(
            nameof(RowCornerRadius),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("ShapeExtraLarge"));

        public static readonly BindableProperty RowPaddingProperty = BindableProperty.Create(
            nameof(RowPadding),
            typeof(Thickness),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Thickness>("M3ActionSheetRowPadding"));

        public static readonly BindableProperty RowMinHeightProperty = BindableProperty.Create(
            nameof(RowMinHeight),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ActionSheetRowMinHeight"));

        public static readonly BindableProperty IconFrameSizeProperty = BindableProperty.Create(
            nameof(IconFrameSize),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ActionSheetRowIconFrameSize"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ActionSheetRowIconSize"));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3LabelLargeFontSize"));

        public static readonly BindableProperty TextLineHeightProperty = BindableProperty.Create(
            nameof(TextLineHeight),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3LabelLargeLineHeight"));

        public static readonly BindableProperty IconFrameBorderWidthProperty = BindableProperty.Create(
            nameof(IconFrameBorderWidth),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty ContentSpacingProperty = BindableProperty.Create(
            nameof(ContentSpacing),
            typeof(double),
            typeof(ActionSheetItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("Space12"));

        private readonly Border _container;
        private readonly Grid _grid;
        private readonly Border _iconFrame;
        private readonly IconView _leadingIcon;
        private readonly Label _label;
        private readonly IconView _selectedIcon;

        public ActionSheetItemView()
        {
            _leadingIcon = new IconView();
            _iconFrame = new Border
            {
                Content = _leadingIcon,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            _label = new Label
            {
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2,
                VerticalOptions = LayoutOptions.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };
            _selectedIcon = new IconView
            {
                IconData = IconPathData.Check,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
            };
            _grid.Add(_iconFrame, 0, 0);
            _grid.Add(_label, 1, 0);
            _grid.Add(_selectedIcon, 2, 0);

            _container = new Border
            {
                StrokeThickness = MaterialResources.Get<double>("M3StrokeNone"),
                Content = _grid,
            };

            Content = _container;
            UpdateVisualState();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

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

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public Color IconColor
        {
            get => (Color)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }

        public Color RowBackgroundColor
        {
            get => (Color)GetValue(RowBackgroundColorProperty);
            set => SetValue(RowBackgroundColorProperty, value);
        }

        public Color PressedRowBackgroundColor
        {
            get => (Color)GetValue(PressedRowBackgroundColorProperty);
            set => SetValue(PressedRowBackgroundColorProperty, value);
        }

        public Color IconFrameBackgroundColor
        {
            get => (Color)GetValue(IconFrameBackgroundColorProperty);
            set => SetValue(IconFrameBackgroundColorProperty, value);
        }

        public Color IconFrameBorderColor
        {
            get => (Color)GetValue(IconFrameBorderColorProperty);
            set => SetValue(IconFrameBorderColorProperty, value);
        }

        public double RowCornerRadius
        {
            get => (double)GetValue(RowCornerRadiusProperty);
            set => SetValue(RowCornerRadiusProperty, value);
        }

        public Thickness RowPadding
        {
            get => (Thickness)GetValue(RowPaddingProperty);
            set => SetValue(RowPaddingProperty, value);
        }

        public double RowMinHeight
        {
            get => (double)GetValue(RowMinHeightProperty);
            set => SetValue(RowMinHeightProperty, value);
        }

        public double IconFrameSize
        {
            get => (double)GetValue(IconFrameSizeProperty);
            set => SetValue(IconFrameSizeProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public double TextLineHeight
        {
            get => (double)GetValue(TextLineHeightProperty);
            set => SetValue(TextLineHeightProperty, value);
        }

        public double IconFrameBorderWidth
        {
            get => (double)GetValue(IconFrameBorderWidthProperty);
            set => SetValue(IconFrameBorderWidthProperty, value);
        }

        public double ContentSpacing
        {
            get => (double)GetValue(ContentSpacingProperty);
            set => SetValue(ContentSpacingProperty, value);
        }

        protected override void OnPressedStateChanged()
        {
            UpdateVisualState();
        }

        protected override void OnCommandStateChanged()
        {
            UpdateVisualState();
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ActionSheetItemView itemView = (ActionSheetItemView)bindable;
            itemView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_container is null || _grid is null || _iconFrame is null || _leadingIcon is null || _label is null || _selectedIcon is null)
            {
                return;
            }

            Opacity = ResolvePressableOpacity(1);
            MinimumHeightRequest = RowMinHeight;

            _container.BackgroundColor = IsPressed ? PressedRowBackgroundColor : RowBackgroundColor;
            _container.Padding = RowPadding;
            _container.MinimumHeightRequest = RowMinHeight;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(RowCornerRadius),
            };
            _grid.ColumnSpacing = ContentSpacing;

            _iconFrame.WidthRequest = IconFrameSize;
            _iconFrame.HeightRequest = IconFrameSize;
            _iconFrame.StrokeThickness = IconFrameBorderWidth;
            _iconFrame.BackgroundColor = IconFrameBackgroundColor;
            _iconFrame.Stroke = new SolidColorBrush(IconFrameBorderColor);
            _iconFrame.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(RowCornerRadius),
            };

            _leadingIcon.IconData = IconData;
            _leadingIcon.IconColor = IconColor;
            _leadingIcon.IconSize = IconSize;

            _label.Text = Text;
            _label.TextColor = TextColor;
            _label.FontSize = TextFontSize;
            _label.LineHeight = TextLineHeight;

            _selectedIcon.IconColor = IconColor;
            _selectedIcon.IconSize = IconSize;
            _selectedIcon.IsVisible = IsSelected;
        }
    }
}
