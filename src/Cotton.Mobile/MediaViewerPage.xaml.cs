// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Cotton.Mobile.Controls;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class MediaViewerPage : DarkViewerPage
    {
        private const double HiddenAudioSurfaceSize = 1;

        private readonly MediaViewerViewModel _viewModel;
        private TimeSpan _mediaDuration;
        private bool _hasMediaEnded;
        private bool _isHandlerDisconnected;
        private bool _isMediaSourceLoaded;

        public MediaViewerPage(MediaViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            _viewModel = viewModel;
            PlayMediaCommand = new Command(PlayMedia);
            InitializeComponent();
            BindingContext = viewModel;
            ConfigurePlaybackSurface();
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

        private async void PlayMedia()
        {
            EnsureMediaSourceLoaded();

            if (_viewModel.IsAudioPreview && MediaPlayer.CurrentState == MediaElementState.Playing)
            {
                MediaPlayer.Pause();
                return;
            }

            try
            {
                if (_viewModel.IsAudioPreview && _hasMediaEnded)
                {
                    await MediaPlayer.SeekTo(TimeSpan.Zero, CancellationToken.None);
                    _hasMediaEnded = false;
                    UpdateAudioPosition(TimeSpan.Zero);
                }

                StartOverlay.IsOverlayVisible = false;
                MediaPlayer.Play();
            }
            catch (Exception exception)
            {
                _viewModel.ReportPlaybackFailure(exception.Message);
            }
        }

        private void ConfigurePlaybackSurface()
        {
            bool isAudioPreview = _viewModel.IsAudioPreview;
            MediaPlayer.ShouldShowPlaybackControls = !isAudioPreview;
            MediaPlayer.InputTransparent = isAudioPreview;
            MediaPlayer.Opacity = isAudioPreview ? 0 : 1;
            if (isAudioPreview)
            {
                MediaPlayer.WidthRequest = HiddenAudioSurfaceSize;
                MediaPlayer.HeightRequest = HiddenAudioSurfaceSize;
                MediaPlayer.HorizontalOptions = LayoutOptions.Start;
                MediaPlayer.VerticalOptions = LayoutOptions.Start;
            }

            StartOverlay.IsOverlayVisible = !isAudioPreview;
            UpdateAudioPlaybackButton(isPlaying: false);
        }

        private void OnMediaOpened(object? sender, EventArgs e)
        {
            if (!_viewModel.IsAudioPreview)
            {
                return;
            }

            RefreshAudioDuration();
            _hasMediaEnded = false;
            UpdateAudioPosition(MediaPlayer.Position);
            _viewModel.ClearPlaybackFailure();
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            if (!_viewModel.IsAudioPreview)
            {
                return;
            }

            RefreshAudioDuration();
            _hasMediaEnded = true;
            UpdateAudioPosition(_mediaDuration);
            UpdateAudioPlaybackButton(isPlaying: false);
        }

        private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
        {
            if (_viewModel.IsAudioPreview)
            {
                UpdateAudioPlaybackButton(isPlaying: false);
            }

            _viewModel.ReportPlaybackFailure(e.ErrorMessage);
        }

        private void OnMediaPositionChanged(object? sender, MediaPositionChangedEventArgs e)
        {
            if (_viewModel.IsAudioPreview)
            {
                RefreshAudioDuration();
                UpdateAudioPosition(e.Position);
            }
        }

        private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
        {
            if (_viewModel.IsAudioPreview)
            {
                RefreshAudioDuration();
                UpdateAudioPlaybackButton(e.NewState == MediaElementState.Playing);
            }
        }

        private async void OnAudioSeekRequested(object? sender, AudioSeekRequestedEventArgs e)
        {
            if (_mediaDuration <= TimeSpan.Zero)
            {
                return;
            }

            try
            {
                await MediaPlayer.SeekTo(e.Position, CancellationToken.None);
                _hasMediaEnded = false;
                UpdateAudioPosition(e.Position);
            }
            catch (Exception exception)
            {
                _viewModel.ReportPlaybackFailure(exception.Message);
            }
        }

        private void UpdateAudioPosition(TimeSpan position)
        {
            TimeSpan normalizedPosition = position < TimeSpan.Zero ? TimeSpan.Zero : position;
            if (_mediaDuration > TimeSpan.Zero && normalizedPosition > _mediaDuration)
            {
                normalizedPosition = _mediaDuration;
            }

            AudioPlayer.Position = normalizedPosition;
        }

        private void RefreshAudioDuration()
        {
            TimeSpan duration = MediaPlayer.Duration > TimeSpan.Zero
                ? MediaPlayer.Duration
                : TimeSpan.Zero;
            if (duration == _mediaDuration)
            {
                return;
            }

            _mediaDuration = duration;
            AudioPlayer.Duration = duration;
        }

        private void UpdateAudioPlaybackButton(bool isPlaying)
        {
            AudioPlayer.IsPlaying = isPlaying;
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

            _mediaDuration = TimeSpan.Zero;
            _hasMediaEnded = false;
            AudioPlayer.Duration = TimeSpan.Zero;
            AudioPlayer.Position = TimeSpan.Zero;
            UpdateAudioPlaybackButton(isPlaying: false);
            StartOverlay.IsOverlayVisible = !_viewModel.IsAudioPreview;

            if (disconnectHandler)
            {
                MediaPlayer.Handler?.DisconnectHandler();
                _isHandlerDisconnected = true;
            }
        }
    }
}
