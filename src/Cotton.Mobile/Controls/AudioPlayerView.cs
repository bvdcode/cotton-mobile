// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Controls
{
    public class AudioSeekRequestedEventArgs : EventArgs
    {
        public AudioSeekRequestedEventArgs(TimeSpan position)
        {
            Position = position;
        }

        public TimeSpan Position { get; }
    }

    public class AudioPlayerView : ContentView
    {
        private const string PlayerStyleResourceKey = "M3AudioPlayerStack";
        private const string ArtworkStyleResourceKey = "M3AudioArtworkFrame";
        private const string IdentityStyleResourceKey = "M3AudioIdentityStack";
        private const string TitleStyleResourceKey = "M3AudioPlayerTitle";
        private const string DetailsStyleResourceKey = "M3AudioPlayerDetails";
        private const string TimelineStackStyleResourceKey = "M3AudioTimelineStack";
        private const string TimelineStyleResourceKey = "M3AudioTimeline";
        private const string ElapsedStyleResourceKey = "M3AudioElapsedTime";
        private const string DurationStyleResourceKey = "M3AudioDuration";
        private const string PlaybackButtonStyleResourceKey = "M3AudioPlaybackIconButton";

        public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
            nameof(TitleText),
            typeof(string),
            typeof(AudioPlayerView),
            string.Empty,
            propertyChanged: OnPresentationPropertyChanged);

        public static readonly BindableProperty DetailsTextProperty = BindableProperty.Create(
            nameof(DetailsText),
            typeof(string),
            typeof(AudioPlayerView),
            string.Empty,
            propertyChanged: OnPresentationPropertyChanged);

        public static readonly BindableProperty PositionProperty = BindableProperty.Create(
            nameof(Position),
            typeof(TimeSpan),
            typeof(AudioPlayerView),
            TimeSpan.Zero,
            propertyChanged: OnTimelinePropertyChanged);

        public static readonly BindableProperty DurationProperty = BindableProperty.Create(
            nameof(Duration),
            typeof(TimeSpan),
            typeof(AudioPlayerView),
            TimeSpan.Zero,
            propertyChanged: OnTimelinePropertyChanged);

        public static readonly BindableProperty IsPlayingProperty = BindableProperty.Create(
            nameof(IsPlaying),
            typeof(bool),
            typeof(AudioPlayerView),
            false,
            propertyChanged: OnPlaybackPropertyChanged);

        public static readonly BindableProperty PlaybackCommandProperty = BindableProperty.Create(
            nameof(PlaybackCommand),
            typeof(ICommand),
            typeof(AudioPlayerView),
            default(ICommand),
            propertyChanged: OnPlaybackPropertyChanged);

        private readonly Label _details;
        private readonly Label _duration;
        private readonly Label _elapsed;
        private readonly IconButton _playbackButton;
        private readonly Slider _timeline;
        private readonly Label _title;
        private bool _isDragging;

        public AudioPlayerView()
        {
            IconFrame artwork = new()
            {
                IconData = IconPathData.Audio,
            };
            artwork.SetDynamicResource(StyleProperty, ArtworkStyleResourceKey);

            _title = new Label();
            _title.SetDynamicResource(StyleProperty, TitleStyleResourceKey);

            _details = new Label();
            _details.SetDynamicResource(StyleProperty, DetailsStyleResourceKey);

            VerticalStackLayout identity = new();
            identity.SetDynamicResource(StyleProperty, IdentityStyleResourceKey);
            identity.Children.Add(_title);
            identity.Children.Add(_details);

            _timeline = new Slider
            {
                Minimum = 0,
                Maximum = 1,
            };
            _timeline.SetDynamicResource(StyleProperty, TimelineStyleResourceKey);
            SemanticProperties.SetDescription(_timeline, "Audio position");
            _timeline.DragStarted += OnTimelineDragStarted;
            _timeline.DragCompleted += OnTimelineDragCompleted;
            _timeline.ValueChanged += OnTimelineValueChanged;

            _elapsed = new Label();
            _elapsed.SetDynamicResource(StyleProperty, ElapsedStyleResourceKey);

            _duration = new Label();
            _duration.SetDynamicResource(StyleProperty, DurationStyleResourceKey);

            Grid timeRow = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
            };
            timeRow.Add(_elapsed);
            timeRow.Add(_duration, 2);

            VerticalStackLayout timelineStack = new();
            timelineStack.SetDynamicResource(StyleProperty, TimelineStackStyleResourceKey);
            timelineStack.Children.Add(_timeline);
            timelineStack.Children.Add(timeRow);

            _playbackButton = new IconButton();
            _playbackButton.SetDynamicResource(StyleProperty, PlaybackButtonStyleResourceKey);

            VerticalStackLayout player = new();
            player.SetDynamicResource(StyleProperty, PlayerStyleResourceKey);
            player.Children.Add(artwork);
            player.Children.Add(identity);
            player.Children.Add(timelineStack);
            player.Children.Add(_playbackButton);

            Content = player;
            UpdatePresentation();
            UpdateTimeline();
            UpdatePlayback();
        }

        public event EventHandler<AudioSeekRequestedEventArgs>? SeekRequested;

        public string TitleText
        {
            get => (string)GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        public string DetailsText
        {
            get => (string)GetValue(DetailsTextProperty);
            set => SetValue(DetailsTextProperty, value);
        }

        public TimeSpan Position
        {
            get => (TimeSpan)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }

        public ICommand? PlaybackCommand
        {
            get => (ICommand?)GetValue(PlaybackCommandProperty);
            set => SetValue(PlaybackCommandProperty, value);
        }

        private static void OnPresentationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((AudioPlayerView)bindable).UpdatePresentation();
        }

        private static void OnTimelinePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((AudioPlayerView)bindable).UpdateTimeline();
        }

        private static void OnPlaybackPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((AudioPlayerView)bindable).UpdatePlayback();
        }

        private void OnTimelineDragStarted(object? sender, EventArgs e)
        {
            _isDragging = true;
        }

        private void OnTimelineDragCompleted(object? sender, EventArgs e)
        {
            _isDragging = false;
            if (Duration <= TimeSpan.Zero)
            {
                return;
            }

            TimeSpan position = TimeSpan.FromSeconds(Duration.TotalSeconds * _timeline.Value);
            SeekRequested?.Invoke(this, new AudioSeekRequestedEventArgs(position));
        }

        private void OnTimelineValueChanged(object? sender, ValueChangedEventArgs e)
        {
            if (!_isDragging || Duration <= TimeSpan.Zero)
            {
                return;
            }

            TimeSpan previewPosition = TimeSpan.FromSeconds(Duration.TotalSeconds * e.NewValue);
            _elapsed.Text = CottonMediaTimeFormatter.Format(previewPosition);
        }

        private void UpdatePresentation()
        {
            if (_title is null || _details is null)
            {
                return;
            }

            _title.Text = TitleText ?? string.Empty;
            _details.Text = DetailsText ?? string.Empty;
        }

        private void UpdateTimeline()
        {
            if (_timeline is null || _elapsed is null || _duration is null || _isDragging)
            {
                return;
            }

            TimeSpan duration = Duration > TimeSpan.Zero ? Duration : TimeSpan.Zero;
            TimeSpan position = Position < TimeSpan.Zero ? TimeSpan.Zero : Position;
            if (duration > TimeSpan.Zero && position > duration)
            {
                position = duration;
            }

            bool hasDuration = duration > TimeSpan.Zero;
            _timeline.IsEnabled = hasDuration;
            _timeline.Value = hasDuration
                ? position.TotalSeconds / duration.TotalSeconds
                : 0;
            _elapsed.Text = CottonMediaTimeFormatter.Format(position);
            _duration.Text = hasDuration
                ? CottonMediaTimeFormatter.Format(duration)
                : "--:--";
        }

        private void UpdatePlayback()
        {
            if (_playbackButton is null)
            {
                return;
            }

            _playbackButton.Command = PlaybackCommand;
            _playbackButton.IconData = IsPlaying ? IconPathData.Pause : IconPathData.Play;
            SemanticProperties.SetDescription(
                _playbackButton,
                IsPlaying ? "Pause audio" : "Play audio");
        }
    }
}
