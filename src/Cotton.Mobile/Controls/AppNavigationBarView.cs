// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class AppNavigationBarView : ContentView
    {
        private const string SelectedItemStyleResourceKey = "M3NavigationBarItemSelected";
        private const string UnselectedItemStyleResourceKey = "M3NavigationBarItemUnselected";

        public static readonly BindableProperty SelectedDestinationProperty = BindableProperty.Create(
            nameof(SelectedDestination),
            typeof(AppNavigationDestination),
            typeof(AppNavigationBarView),
            AppNavigationDestination.Sync,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SyncCommandProperty = BindableProperty.Create(
            nameof(SyncCommand),
            typeof(ICommand),
            typeof(AppNavigationBarView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ProfileCommandProperty = BindableProperty.Create(
            nameof(ProfileCommand),
            typeof(ICommand),
            typeof(AppNavigationBarView),
            propertyChanged: OnVisualPropertyChanged);

        private readonly NavigationBarItem _profileItem;
        private readonly NavigationBarItem _syncItem;

        public AppNavigationBarView()
        {
            _syncItem = CreateItem("Sync", IconPathData.Transfer, 0, "Open sync");
            _profileItem = CreateItem("Profile", IconPathData.Profile, 1, "Open profile");

            Content = new NavigationBarView
            {
                ColumnCount = 2,
                Items =
                {
                    _syncItem,
                    _profileItem,
                },
            };

            UpdateVisualState();
        }

        public AppNavigationDestination SelectedDestination
        {
            get => (AppNavigationDestination)GetValue(SelectedDestinationProperty);
            set => SetValue(SelectedDestinationProperty, value);
        }

        public ICommand? SyncCommand
        {
            get => (ICommand?)GetValue(SyncCommandProperty);
            set => SetValue(SyncCommandProperty, value);
        }

        public ICommand? ProfileCommand
        {
            get => (ICommand?)GetValue(ProfileCommandProperty);
            set => SetValue(ProfileCommandProperty, value);
        }

        private static NavigationBarItem CreateItem(
            string text,
            Geometry iconData,
            int column,
            string semanticDescription)
        {
            NavigationBarItem item = new()
            {
                Text = text,
                IconData = iconData,
            };
            Grid.SetColumn(item, column);
            SemanticProperties.SetDescription(item, semanticDescription);
            return item;
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AppNavigationBarView view = (AppNavigationBarView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _syncItem.Command = SyncCommand;
            _profileItem.Command = ProfileCommand;

            switch (SelectedDestination)
            {
                case AppNavigationDestination.Sync:
                    ApplySelection(_syncItem, isSelected: true);
                    ApplySelection(_profileItem, isSelected: false);
                    break;
                case AppNavigationDestination.Profile:
                    ApplySelection(_syncItem, isSelected: false);
                    ApplySelection(_profileItem, isSelected: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(SelectedDestination),
                        "App navigation destination is not supported.");
            }
        }

        private static void ApplySelection(NavigationBarItem item, bool isSelected)
        {
            item.SetDynamicResource(
                StyleProperty,
                isSelected ? SelectedItemStyleResourceKey : UnselectedItemStyleResourceKey);
        }
    }
}
