// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SelectionBarView : ContentView
    {
        private const string DefaultActionClusterStyleResourceKey = "M3SelectionBarActionCluster";
        private const string DefaultIconButtonStyleResourceKey = "M3FileChromeIconButton";

        public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
            nameof(TitleText),
            typeof(string),
            typeof(SelectionBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(SelectionBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionIconDataProperty = BindableProperty.Create(
            nameof(PrimaryActionIconData),
            typeof(Geometry),
            typeof(SelectionBarView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionCommandProperty = BindableProperty.Create(
            nameof(PrimaryActionCommand),
            typeof(ICommand),
            typeof(SelectionBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionIsEnabledProperty = BindableProperty.Create(
            nameof(PrimaryActionIsEnabled),
            typeof(bool),
            typeof(SelectionBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(PrimaryActionSemanticDescription),
            typeof(string),
            typeof(SelectionBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(SelectionBarView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconDataProperty = BindableProperty.Create(
            nameof(SecondaryActionIconData),
            typeof(Geometry),
            typeof(SelectionBarView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionCommandProperty = BindableProperty.Create(
            nameof(SecondaryActionCommand),
            typeof(ICommand),
            typeof(SelectionBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIsEnabledProperty = BindableProperty.Create(
            nameof(SecondaryActionIsEnabled),
            typeof(bool),
            typeof(SelectionBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(SecondaryActionSemanticDescription),
            typeof(string),
            typeof(SelectionBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(SelectionBarView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionIconDataProperty = BindableProperty.Create(
            nameof(TertiaryActionIconData),
            typeof(Geometry),
            typeof(SelectionBarView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionCommandProperty = BindableProperty.Create(
            nameof(TertiaryActionCommand),
            typeof(ICommand),
            typeof(SelectionBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionIsEnabledProperty = BindableProperty.Create(
            nameof(TertiaryActionIsEnabled),
            typeof(bool),
            typeof(SelectionBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(TertiaryActionSemanticDescription),
            typeof(string),
            typeof(SelectionBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TertiaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(SelectionBarView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _container;
        private readonly Label _detailLabel;
        private readonly Grid _grid;
        private readonly ActionClusterView _actions;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _titleLabel;

        public SelectionBarView()
        {
            _titleLabel = new Label();
            _detailLabel = new Label();

            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _titleLabel,
                    _detailLabel,
                },
            };

            _actions = new ActionClusterView
            {
                ClusterStyleResourceKey = DefaultActionClusterStyleResourceKey,
            };
            Grid.SetColumn(_actions, 1);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Star,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                },
                Children =
                {
                    _textStack,
                    _actions,
                },
            };

            _container = new Border
            {
                Content = _grid,
            };

            Content = _container;
            UpdateVisualState();
        }

        public string TitleText
        {
            get => (string)GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public Geometry? PrimaryActionIconData
        {
            get => (Geometry?)GetValue(PrimaryActionIconDataProperty);
            set => SetValue(PrimaryActionIconDataProperty, value);
        }

        public ICommand? PrimaryActionCommand
        {
            get => (ICommand?)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }

        public bool PrimaryActionIsEnabled
        {
            get => (bool)GetValue(PrimaryActionIsEnabledProperty);
            set => SetValue(PrimaryActionIsEnabledProperty, value);
        }

        public string PrimaryActionSemanticDescription
        {
            get => (string)GetValue(PrimaryActionSemanticDescriptionProperty);
            set => SetValue(PrimaryActionSemanticDescriptionProperty, value);
        }

        public string PrimaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(PrimaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(PrimaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public Geometry? SecondaryActionIconData
        {
            get => (Geometry?)GetValue(SecondaryActionIconDataProperty);
            set => SetValue(SecondaryActionIconDataProperty, value);
        }

        public ICommand? SecondaryActionCommand
        {
            get => (ICommand?)GetValue(SecondaryActionCommandProperty);
            set => SetValue(SecondaryActionCommandProperty, value);
        }

        public bool SecondaryActionIsEnabled
        {
            get => (bool)GetValue(SecondaryActionIsEnabledProperty);
            set => SetValue(SecondaryActionIsEnabledProperty, value);
        }

        public string SecondaryActionSemanticDescription
        {
            get => (string)GetValue(SecondaryActionSemanticDescriptionProperty);
            set => SetValue(SecondaryActionSemanticDescriptionProperty, value);
        }

        public string SecondaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(SecondaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(SecondaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public Geometry? TertiaryActionIconData
        {
            get => (Geometry?)GetValue(TertiaryActionIconDataProperty);
            set => SetValue(TertiaryActionIconDataProperty, value);
        }

        public ICommand? TertiaryActionCommand
        {
            get => (ICommand?)GetValue(TertiaryActionCommandProperty);
            set => SetValue(TertiaryActionCommandProperty, value);
        }

        public bool TertiaryActionIsEnabled
        {
            get => (bool)GetValue(TertiaryActionIsEnabledProperty);
            set => SetValue(TertiaryActionIsEnabledProperty, value);
        }

        public string TertiaryActionSemanticDescription
        {
            get => (string)GetValue(TertiaryActionSemanticDescriptionProperty);
            set => SetValue(TertiaryActionSemanticDescriptionProperty, value);
        }

        public string TertiaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(TertiaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(TertiaryActionIconButtonStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SelectionBarView view = (SelectionBarView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _container.SetDynamicResource(StyleProperty, "M3SelectionBar");
            _grid.SetDynamicResource(StyleProperty, "M3SelectionBarGrid");
            _textStack.SetDynamicResource(StyleProperty, "M3SelectionTextStack");
            _titleLabel.SetDynamicResource(StyleProperty, "M3SelectionTitleText");
            _detailLabel.SetDynamicResource(StyleProperty, "M3SelectionDetailText");

            _titleLabel.Text = TitleText ?? string.Empty;
            _detailLabel.Text = DetailText ?? string.Empty;

            _actions.ClusterStyleResourceKey = DefaultActionClusterStyleResourceKey;
            _actions.PrimaryActionIconData = PrimaryActionIconData;
            _actions.PrimaryActionCommand = PrimaryActionCommand;
            _actions.PrimaryActionIconButtonStyleResourceKey =
                ResolveIconButtonStyleResourceKey(PrimaryActionIconButtonStyleResourceKey);
            _actions.PrimaryActionSemanticDescription = PrimaryActionSemanticDescription ?? string.Empty;
            _actions.IsPrimaryActionEnabled = PrimaryActionIsEnabled;
            _actions.IsPrimaryActionVisible = true;
            _actions.SecondaryActionIconData = SecondaryActionIconData;
            _actions.SecondaryActionCommand = SecondaryActionCommand;
            _actions.SecondaryActionIconButtonStyleResourceKey =
                ResolveIconButtonStyleResourceKey(SecondaryActionIconButtonStyleResourceKey);
            _actions.SecondaryActionSemanticDescription = SecondaryActionSemanticDescription ?? string.Empty;
            _actions.IsSecondaryActionEnabled = SecondaryActionIsEnabled;
            _actions.IsSecondaryActionVisible = true;
            _actions.TertiaryActionIconData = TertiaryActionIconData;
            _actions.TertiaryActionCommand = TertiaryActionCommand;
            _actions.TertiaryActionIconButtonStyleResourceKey =
                ResolveIconButtonStyleResourceKey(TertiaryActionIconButtonStyleResourceKey);
            _actions.TertiaryActionSemanticDescription = TertiaryActionSemanticDescription ?? string.Empty;
            _actions.IsTertiaryActionEnabled = TertiaryActionIsEnabled;
            _actions.IsTertiaryActionVisible = true;
            _actions.IsQuaternaryActionVisible = false;
        }

        private static string ResolveIconButtonStyleResourceKey(string iconButtonStyleResourceKey)
        {
            return string.IsNullOrWhiteSpace(iconButtonStyleResourceKey)
                ? DefaultIconButtonStyleResourceKey
                : iconButtonStyleResourceKey;
        }
    }
}
