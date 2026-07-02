// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class AuthLegalFooterView : ContentView
    {
        private const string DefaultFooterStyleResourceKey = "M3LegalFooterBar";
        private const string DefaultPrivacyText = "Privacy";

        public static readonly BindableProperty PrivacyTextProperty = BindableProperty.Create(
            nameof(PrivacyText),
            typeof(string),
            typeof(AuthLegalFooterView),
            DefaultPrivacyText,
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

        private readonly HorizontalStackLayout _footer;
        private readonly TextAction _privacyAction;

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
            UpdateVisualState();
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthLegalFooterView view = (AuthLegalFooterView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string footerStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                FooterStyleResourceKey,
                DefaultFooterStyleResourceKey);

            _footer.SetDynamicResource(StyleProperty, footerStyleResourceKey);
            _privacyAction.Text = PrivacyText ?? string.Empty;
            _privacyAction.Command = PrivacyCommand;
        }
    }
}
