// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Controls
{
    public class AuthLegalFooterView : ContentView
    {
        private const string DefaultContainerStyleResourceKey = "M3AuthLegalFooterContainer";
        private const string DefaultFooterStyleResourceKey = "M3LegalFooterBar";
        private const string FooterOpacityAnimationName = "M3AuthLegalFooterOpacity";

        public static readonly BindableProperty PrivacyTextProperty = BindableProperty.Create(
            nameof(PrivacyText),
            typeof(string),
            typeof(AuthLegalFooterView),
            AppResources.PrivacyText,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrivacyCommandProperty = BindableProperty.Create(
            nameof(PrivacyCommand),
            typeof(ICommand),
            typeof(AuthLegalFooterView),
            default(ICommand),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty FooterStyleResourceKeyProperty = BindableProperty.Create(
            nameof(FooterStyleResourceKey),
            typeof(string),
            typeof(AuthLegalFooterView),
            DefaultFooterStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsFooterVisibleProperty = BindableProperty.Create(
            nameof(IsFooterVisible),
            typeof(bool),
            typeof(AuthLegalFooterView),
            true,
            propertyChanged: OnFooterVisiblePropertyChanged);

        private readonly HorizontalStackLayout _footer;
        private readonly TextAction _privacyAction;
        private bool _hasAppliedFooterVisibility;

        public AuthLegalFooterView()
        {
            _privacyAction = new TextAction();
            _footer = new HorizontalStackLayout
            {
                Children =
                {
                    _privacyAction,
                },
            };

            Content = _footer;
            SetDynamicResource(StyleProperty, DefaultContainerStyleResourceKey);
            UpdateVisualState();
            UpdateFooterVisibility(animateFooterVisibility: false);
            UpdateInputTransparency();
        }

        public string PrivacyText
        {
            get => (string)GetValue(PrivacyTextProperty);
            set => SetValue(PrivacyTextProperty, value);
        }

        public ICommand? PrivacyCommand
        {
            get => (ICommand?)GetValue(PrivacyCommandProperty);
            set => SetValue(PrivacyCommandProperty, value);
        }

        public string FooterStyleResourceKey
        {
            get => (string)GetValue(FooterStyleResourceKeyProperty);
            set => SetValue(FooterStyleResourceKeyProperty, value);
        }

        public bool IsFooterVisible
        {
            get => (bool)GetValue(IsFooterVisibleProperty);
            set => SetValue(IsFooterVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthLegalFooterView view = (AuthLegalFooterView)bindable;
            view.UpdateVisualState();
        }

        private static void OnFooterVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthLegalFooterView view = (AuthLegalFooterView)bindable;
            view.UpdateFooterVisibility(animateFooterVisibility: true);
        }

        private void UpdateVisualState()
        {
            string footerStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                FooterStyleResourceKey,
                DefaultFooterStyleResourceKey);

            _footer.SetDynamicResource(StyleProperty, footerStyleResourceKey);
            _privacyAction.Text = PrivacyText ?? string.Empty;
            _privacyAction.Command = PrivacyCommand;
            UpdateInputTransparency();
        }

        private void UpdateFooterVisibility(bool animateFooterVisibility)
        {
            bool shouldAnimate = animateFooterVisibility && _hasAppliedFooterVisibility;
            MaterialVisibility.Update(
                this,
                IsFooterVisible,
                FooterOpacityAnimationName,
                shouldAnimate,
                UpdateInputTransparency);
            _hasAppliedFooterVisibility = true;
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsVisible || !IsFooterVisible || PrivacyCommand is null;
        }
    }
}
