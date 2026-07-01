// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class MediaViewerPage : ContentPage
    {
        private readonly MediaViewerViewModel _viewModel;
        private bool _isHandlerDisconnected;
        private bool _isMediaSourceLoaded;

        public MediaViewerPage(MediaViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            _viewModel = viewModel;
            InitializeComponent();
            PlayMediaCommand = new Command(PlayMedia);
            BindingContext = viewModel;
            Unloaded += OnPageUnloaded;
        }

        public ICommand PlayMediaCommand { get; }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            EnsureMediaSourceLoaded();
        }

        protected override void OnDisappearing()
        {
            ReleaseMediaPlayer(disconnectHandler: false);

            base.OnDisappearing();
        }

        private void PlayMedia()
        {
            EnsureMediaSourceLoaded();
            StartOverlay.IsVisible = false;
            MediaPlayer.Play();
        }

        private void OnPageUnloaded(object? sender, EventArgs e)
        {
            ReleaseMediaPlayer(disconnectHandler: true);
            Unloaded -= OnPageUnloaded;
        }

        private void EnsureMediaSourceLoaded()
        {
            if (_isHandlerDisconnected || _isMediaSourceLoaded)
            {
                return;
            }

            MediaPlayer.Source = MediaSource.FromFile(_viewModel.MediaFilePath);
            _isMediaSourceLoaded = true;
        }

        private void ReleaseMediaPlayer(bool disconnectHandler)
        {
            if (_isHandlerDisconnected)
            {
                return;
            }

            if (_isMediaSourceLoaded)
            {
                MediaPlayer.Stop();
                MediaPlayer.Source = null;
                _isMediaSourceLoaded = false;
            }

            StartOverlay.IsVisible = true;

            if (disconnectHandler)
            {
                MediaPlayer.Handler?.DisconnectHandler();
                _isHandlerDisconnected = true;
            }
        }
    }
}
