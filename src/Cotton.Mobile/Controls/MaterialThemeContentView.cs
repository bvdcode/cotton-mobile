// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public abstract class MaterialThemeContentView : ContentView
    {
        private Application? _observedApplication;

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler is null)
            {
                DetachRequestedThemeChanged();
                return;
            }

            AttachRequestedThemeChanged();
        }

        protected virtual void OnRequestedThemeChanged(AppThemeChangedEventArgs e)
        {
        }

        private void AttachRequestedThemeChanged()
        {
            Application? currentApplication = Application.Current;
            if (ReferenceEquals(_observedApplication, currentApplication))
            {
                return;
            }

            DetachRequestedThemeChanged();

            if (currentApplication is null)
            {
                return;
            }

            _observedApplication = currentApplication;
            currentApplication.RequestedThemeChanged += HandleRequestedThemeChanged;
        }

        private void DetachRequestedThemeChanged()
        {
            if (_observedApplication is null)
            {
                return;
            }

            _observedApplication.RequestedThemeChanged -= HandleRequestedThemeChanged;
            _observedApplication = null;
        }

        private void HandleRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            OnRequestedThemeChanged(e);
        }
    }
}
