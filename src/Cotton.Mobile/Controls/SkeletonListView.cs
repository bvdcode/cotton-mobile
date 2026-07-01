// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public abstract class SkeletonListView : VerticalStackLayout
    {
        private const string AppearanceAnimationName = nameof(SkeletonListView) + "Appearance";

        public static readonly BindableProperty AppearanceDurationProperty = BindableProperty.Create(
            nameof(AppearanceDuration),
            typeof(int),
            typeof(SkeletonListView),
            propertyChanged: OnAppearancePropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<int>("M3MotionSkeletonEnterDuration"));

        public static readonly BindableProperty RowCountProperty = BindableProperty.Create(
            nameof(RowCount),
            typeof(int),
            typeof(SkeletonListView),
            3,
            propertyChanged: OnRowCountChanged);

        private bool _isLoaded;

        protected SkeletonListView()
        {
            InputTransparent = true;
            Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public int AppearanceDuration
        {
            get => (int)GetValue(AppearanceDurationProperty);
            set => SetValue(AppearanceDurationProperty, value);
        }

        public int RowCount
        {
            get => (int)GetValue(RowCountProperty);
            set => SetValue(RowCountProperty, value);
        }

        protected abstract View CreateRow();

        protected void RebuildRows()
        {
            Children.Clear();

            int rowCount = Math.Max(0, RowCount);
            for (int index = 0; index < rowCount; index++)
            {
                Children.Add(CreateRow());
            }
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsVisible), StringComparison.Ordinal))
            {
                UpdateAppearanceState();
            }
        }

        private static void OnAppearancePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SkeletonListView skeletonListView = (SkeletonListView)bindable;
            skeletonListView.UpdateAppearanceState();
        }

        private static void OnRowCountChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SkeletonListView skeletonListView = (SkeletonListView)bindable;
            skeletonListView.RebuildRows();
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            _isLoaded = true;
            UpdateAppearanceState();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            _isLoaded = false;
            this.AbortAnimation(AppearanceAnimationName);
            Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
        }

        private void UpdateAppearanceState()
        {
            this.AbortAnimation(AppearanceAnimationName);

            if (!_isLoaded || !IsVisible)
            {
                Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
                return;
            }

            int duration = AppearanceDuration;
            if (duration <= 0)
            {
                Opacity = MaterialMotion.Value("M3MotionVisibleOpacity");
                return;
            }

            Animation animation = new(
                value => Opacity = value,
                MaterialMotion.Value("M3MotionHiddenOpacity"),
                MaterialMotion.Value("M3MotionVisibleOpacity"),
                Easing.CubicOut);
            animation.Commit(
                this,
                AppearanceAnimationName,
                length: MaterialMotion.Duration(duration));
        }
    }
}
