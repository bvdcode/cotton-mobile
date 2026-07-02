// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class FileBrowserTopBarView : ContentView
    {
        private const string DefaultActionsContainerStyleResourceKey = "M3FileBrowserActionsContainer";
        private const string DefaultActionClusterStyleResourceKey = "M3FileBrowserActionCluster";
        private const string DefaultActionIconButtonStyleResourceKey = "M3FileBrowserTopBarIconButton";
        private const string DefaultDetailTextStyleResourceKey = "M3CardMetaLine";
        private const string DefaultGridStyleResourceKey = "M3FileBrowserTopBar";
        private const string DefaultTitleStackStyleResourceKey = "M3FileBrowserTitleStack";
        private const string DefaultTitleTextStyleResourceKey = "M3BrowserTitleLine";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FileBrowserTopBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PathTextProperty = BindableProperty.Create(
            nameof(PathText),
            typeof(string),
            typeof(FileBrowserTopBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusTextProperty = BindableProperty.Create(
            nameof(StatusText),
            typeof(string),
            typeof(FileBrowserTopBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPathTextVisibleProperty = BindableProperty.Create(
            nameof(IsPathTextVisible),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusTextVisibleProperty = BindableProperty.Create(
            nameof(IsStatusTextVisible),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty NavigateUpCommandProperty = BindableProperty.Create(
            nameof(NavigateUpCommand),
            typeof(ICommand),
            typeof(FileBrowserTopBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsNavigateUpEnabledProperty = BindableProperty.Create(
            nameof(IsNavigateUpEnabled),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsNavigateUpVisibleProperty = BindableProperty.Create(
            nameof(IsNavigateUpVisible),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty NavigateUpButtonOpacityProperty = BindableProperty.Create(
            nameof(NavigateUpButtonOpacity),
            typeof(double),
            typeof(FileBrowserTopBarView),
            1d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SearchCommandProperty = BindableProperty.Create(
            nameof(SearchCommand),
            typeof(ICommand),
            typeof(FileBrowserTopBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SearchSemanticDescriptionProperty = BindableProperty.Create(
            nameof(SearchSemanticDescription),
            typeof(string),
            typeof(FileBrowserTopBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSearchActiveProperty = BindableProperty.Create(
            nameof(IsSearchActive),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsChromeEnabledProperty = BindableProperty.Create(
            nameof(IsChromeEnabled),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SortCommandProperty = BindableProperty.Create(
            nameof(SortCommand),
            typeof(ICommand),
            typeof(FileBrowserTopBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSortVisibleProperty = BindableProperty.Create(
            nameof(IsSortVisible),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ViewCommandProperty = BindableProperty.Create(
            nameof(ViewCommand),
            typeof(ICommand),
            typeof(FileBrowserTopBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsViewVisibleProperty = BindableProperty.Create(
            nameof(IsViewVisible),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ProfileInitialsProperty = BindableProperty.Create(
            nameof(ProfileInitials),
            typeof(string),
            typeof(FileBrowserTopBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty AccountCommandProperty = BindableProperty.Create(
            nameof(AccountCommand),
            typeof(ICommand),
            typeof(FileBrowserTopBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsAccountEnabledProperty = BindableProperty.Create(
            nameof(IsAccountEnabled),
            typeof(bool),
            typeof(FileBrowserTopBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStackStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultTitleStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleTextStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultTitleTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailTextStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionClusterStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionClusterStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultActionClusterStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionsContainerStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionsContainerStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultActionsContainerStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(FileBrowserTopBarView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly InitialsButton _accountButton;
        private readonly ActionClusterView _actionCluster;
        private readonly HorizontalStackLayout _actionsContainer;
        private readonly Grid _grid;
        private readonly Label _pathText;
        private readonly Label _statusText;
        private readonly Label _titleText;
        private readonly VerticalStackLayout _titleStack;
        private readonly IconButton _upButton;

        public FileBrowserTopBarView()
        {
            _upButton = new IconButton
            {
                IconData = IconPathData.ArrowUp,
            };
            _titleText = new Label();
            _pathText = new Label();
            _statusText = new Label();
            _titleStack = new VerticalStackLayout
            {
                Children =
                {
                    _titleText,
                    _pathText,
                    _statusText,
                },
            };
            _actionCluster = new ActionClusterView();
            _accountButton = new InitialsButton();
            _actionsContainer = new HorizontalStackLayout
            {
                Children =
                {
                    _actionCluster,
                    _accountButton,
                },
            };

            Grid.SetColumn(_titleStack, 1);
            Grid.SetColumn(_actionsContainer, 2);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _upButton,
                    _titleStack,
                    _actionsContainer,
                },
            };

            Content = _grid;
            UpdateVisualState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string PathText
        {
            get => (string)GetValue(PathTextProperty);
            set => SetValue(PathTextProperty, value);
        }

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public bool IsPathTextVisible
        {
            get => (bool)GetValue(IsPathTextVisibleProperty);
            set => SetValue(IsPathTextVisibleProperty, value);
        }

        public bool IsStatusTextVisible
        {
            get => (bool)GetValue(IsStatusTextVisibleProperty);
            set => SetValue(IsStatusTextVisibleProperty, value);
        }

        public ICommand? NavigateUpCommand
        {
            get => (ICommand?)GetValue(NavigateUpCommandProperty);
            set => SetValue(NavigateUpCommandProperty, value);
        }

        public bool IsNavigateUpEnabled
        {
            get => (bool)GetValue(IsNavigateUpEnabledProperty);
            set => SetValue(IsNavigateUpEnabledProperty, value);
        }

        public bool IsNavigateUpVisible
        {
            get => (bool)GetValue(IsNavigateUpVisibleProperty);
            set => SetValue(IsNavigateUpVisibleProperty, value);
        }

        public double NavigateUpButtonOpacity
        {
            get => (double)GetValue(NavigateUpButtonOpacityProperty);
            set => SetValue(NavigateUpButtonOpacityProperty, value);
        }

        public ICommand? SearchCommand
        {
            get => (ICommand?)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public string SearchSemanticDescription
        {
            get => (string)GetValue(SearchSemanticDescriptionProperty);
            set => SetValue(SearchSemanticDescriptionProperty, value);
        }

        public bool IsSearchActive
        {
            get => (bool)GetValue(IsSearchActiveProperty);
            set => SetValue(IsSearchActiveProperty, value);
        }

        public bool IsChromeEnabled
        {
            get => (bool)GetValue(IsChromeEnabledProperty);
            set => SetValue(IsChromeEnabledProperty, value);
        }

        public ICommand? SortCommand
        {
            get => (ICommand?)GetValue(SortCommandProperty);
            set => SetValue(SortCommandProperty, value);
        }

        public bool IsSortVisible
        {
            get => (bool)GetValue(IsSortVisibleProperty);
            set => SetValue(IsSortVisibleProperty, value);
        }

        public ICommand? ViewCommand
        {
            get => (ICommand?)GetValue(ViewCommandProperty);
            set => SetValue(ViewCommandProperty, value);
        }

        public bool IsViewVisible
        {
            get => (bool)GetValue(IsViewVisibleProperty);
            set => SetValue(IsViewVisibleProperty, value);
        }

        public string ProfileInitials
        {
            get => (string)GetValue(ProfileInitialsProperty);
            set => SetValue(ProfileInitialsProperty, value);
        }

        public ICommand? AccountCommand
        {
            get => (ICommand?)GetValue(AccountCommandProperty);
            set => SetValue(AccountCommandProperty, value);
        }

        public bool IsAccountEnabled
        {
            get => (bool)GetValue(IsAccountEnabledProperty);
            set => SetValue(IsAccountEnabledProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string TitleStackStyleResourceKey
        {
            get => (string)GetValue(TitleStackStyleResourceKeyProperty);
            set => SetValue(TitleStackStyleResourceKeyProperty, value);
        }

        public string TitleTextStyleResourceKey
        {
            get => (string)GetValue(TitleTextStyleResourceKeyProperty);
            set => SetValue(TitleTextStyleResourceKeyProperty, value);
        }

        public string DetailTextStyleResourceKey
        {
            get => (string)GetValue(DetailTextStyleResourceKeyProperty);
            set => SetValue(DetailTextStyleResourceKeyProperty, value);
        }

        public string ActionClusterStyleResourceKey
        {
            get => (string)GetValue(ActionClusterStyleResourceKeyProperty);
            set => SetValue(ActionClusterStyleResourceKeyProperty, value);
        }

        public string ActionsContainerStyleResourceKey
        {
            get => (string)GetValue(ActionsContainerStyleResourceKeyProperty);
            set => SetValue(ActionsContainerStyleResourceKeyProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileBrowserTopBarView view = (FileBrowserTopBarView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string titleStackStyleResourceKey =
                ResolveStyleResourceKey(TitleStackStyleResourceKey, DefaultTitleStackStyleResourceKey);
            string titleTextStyleResourceKey =
                ResolveStyleResourceKey(TitleTextStyleResourceKey, DefaultTitleTextStyleResourceKey);
            string detailTextStyleResourceKey =
                ResolveStyleResourceKey(DetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string actionClusterStyleResourceKey =
                ResolveStyleResourceKey(ActionClusterStyleResourceKey, DefaultActionClusterStyleResourceKey);
            string actionsContainerStyleResourceKey =
                ResolveStyleResourceKey(ActionsContainerStyleResourceKey, DefaultActionsContainerStyleResourceKey);
            string actionIconButtonStyleResourceKey =
                ResolveStyleResourceKey(ActionIconButtonStyleResourceKey, DefaultActionIconButtonStyleResourceKey);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _titleStack.SetDynamicResource(StyleProperty, titleStackStyleResourceKey);
            _titleText.SetDynamicResource(StyleProperty, titleTextStyleResourceKey);
            _pathText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _statusText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _actionsContainer.SetDynamicResource(StyleProperty, actionsContainerStyleResourceKey);
            _upButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _upButton.Command = NavigateUpCommand;
            _upButton.IsEnabled = IsNavigateUpEnabled;
            _upButton.IsVisible = IsNavigateUpVisible;
            _upButton.ButtonOpacity = NavigateUpButtonOpacity;
            SemanticProperties.SetDescription(_upButton, "Up");

            _titleText.Text = Title ?? string.Empty;
            _pathText.Text = PathText ?? string.Empty;
            _pathText.IsVisible = IsPathTextVisible;
            _statusText.Text = StatusText ?? string.Empty;
            _statusText.IsVisible = IsStatusTextVisible;

            _actionCluster.ClusterStyleResourceKey = actionClusterStyleResourceKey;
            _actionCluster.PrimaryActionIconData = IsSearchActive ? IconPathData.Close : IconPathData.Search;
            _actionCluster.PrimaryActionCommand = SearchCommand;
            _actionCluster.PrimaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            _actionCluster.PrimaryActionSemanticDescription = SearchSemanticDescription ?? string.Empty;
            _actionCluster.IsPrimaryActionEnabled = IsChromeEnabled;
            _actionCluster.SecondaryActionIconData = IconPathData.Sort;
            _actionCluster.SecondaryActionCommand = SortCommand;
            _actionCluster.SecondaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            _actionCluster.SecondaryActionSemanticDescription = "Sort files";
            _actionCluster.IsSecondaryActionEnabled = IsChromeEnabled;
            _actionCluster.IsSecondaryActionVisible = IsSortVisible;
            _actionCluster.TertiaryActionIconData = IconPathData.ViewTiles;
            _actionCluster.TertiaryActionCommand = ViewCommand;
            _actionCluster.TertiaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            _actionCluster.TertiaryActionSemanticDescription = "Change file view";
            _actionCluster.IsTertiaryActionEnabled = IsChromeEnabled;
            _actionCluster.IsTertiaryActionVisible = IsViewVisible;

            _accountButton.Text = ProfileInitials ?? string.Empty;
            _accountButton.Command = AccountCommand;
            _accountButton.IsEnabled = IsAccountEnabled;
            SemanticProperties.SetDescription(_accountButton, "Account");
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey)
                ? defaultResourceKey
                : resourceKey;
        }
    }
}
