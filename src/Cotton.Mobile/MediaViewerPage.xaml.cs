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

        private readonly bool _hasVideoPoster;
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
            _hasVideoPoster = !string.IsNullOrWhiteSpace(viewModel.VideoPosterSource);
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
                if (_hasMediaEnded)
                {
                    await MediaPlayer.SeekTo(TimeSpan.Zero, CancellationToken.None);
                    _hasMediaEnded = false;
                    if (_viewModel.IsAudioPreview)
                    {
                        UpdateAudioPosition(TimeSpan.Zero);
                    }
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
            MediaPlayer.ShouldAutoPlay = !isAudioPreview;
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
            ShowVideoPoster(!isAudioPreview);
            UpdateAudioPlaybackButton(isPlaying: false);
        }

        private void OnMediaOpened(object? sender, EventArgs e)
        {
            _hasMediaEnded = false;
            _viewModel.ClearPlaybackFailure();
            if (!_viewModel.IsAudioPreview)
            {
                StartOverlay.IsOverlayVisible = false;
                return;
            }

            RefreshAudioDuration();
            UpdateAudioPosition(MediaPlayer.Position);
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            _hasMediaEnded = true;
            if (!_viewModel.IsAudioPreview)
            {
                StartOverlay.IsOverlayVisible = true;
                ShowVideoPoster(true);
                return;
            }

            RefreshAudioDuration();
            UpdateAudioPosition(_mediaDuration);
            UpdateAudioPlaybackButton(isPlaying: false);
        }

        private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
        {
            if (_viewModel.IsAudioPreview)
            {
                UpdateAudioPlaybackButton(isPlaying: false);
            }
            else
            {
                StartOverlay.IsOverlayVisible = true;
                ShowVideoPoster(true);
            }

            _viewModel.ReportPlaybackFailure(e.ErrorMessage);
        }

        private void OnMediaPositionChanged(object? sender, MediaPositionChangedEventArgs e)
        {
            if (!_viewModel.IsAudioPreview)
            {
                if (e.Position > TimeSpan.Zero)
                {
                    ShowVideoPoster(false);
                }

                return;
            }

            RefreshAudioDuration();
            UpdateAudioPosition(e.Position);
        }

        private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
        {
            if (!_viewModel.IsAudioPreview)
            {
                if (e.NewState == MediaElementState.Playing)
                {
                    StartOverlay.IsOverlayVisible = false;
                }

                return;
            }

            RefreshAudioDuration();
            UpdateAudioPlaybackButton(e.NewState == MediaElementState.Playing);
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
            ShowVideoPoster(!_viewModel.IsAudioPreview);

            if (disconnectHandler)
            {
                MediaPlayer.Handler?.DisconnectHandler();
                _isHandlerDisconnected = true;
            }
        }

        private void ShowVideoPoster(bool isVisible)
        {
            VideoPoster.IsVisible = _hasVideoPoster && isVisible;
        }
    }
}
