namespace Cotton.Mobile.Services
{
    public class CottonAppLockPolicy
    {
        public CottonAppLockPolicy(TimeSpan backgroundTimeout)
        {
            if (backgroundTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(backgroundTimeout),
                    "App lock timeout cannot be negative.");
            }

            BackgroundTimeout = backgroundTimeout;
        }

        public TimeSpan BackgroundTimeout { get; }

        public string BackgroundTimeoutText => FormatDuration(BackgroundTimeout);

        public static CottonAppLockPolicy Default { get; } = new(TimeSpan.FromSeconds(30));

        public bool ShouldLock(
            CottonAppLockSettings settings,
            CottonAppLockCapabilitySnapshot capability,
            CottonAppLockRuntimeState runtimeState,
            DateTimeOffset nowUtc)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(capability);
            ArgumentNullException.ThrowIfNull(runtimeState);

            if (!settings.IsEnabled || !capability.CanEnable)
            {
                return false;
            }

            DateTimeOffset? lastBackgroundedAtUtc = runtimeState.LastBackgroundedAtUtc;
            if (lastBackgroundedAtUtc is null)
            {
                return false;
            }

            DateTimeOffset now = nowUtc.ToUniversalTime();
            if (runtimeState.LastUnlockedAtUtc is not null
                && runtimeState.LastUnlockedAtUtc.Value >= lastBackgroundedAtUtc.Value)
            {
                return false;
            }

            return now - lastBackgroundedAtUtc.Value >= BackgroundTimeout;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
            {
                return "immediately";
            }

            if (duration.TotalSeconds < 60)
            {
                int seconds = (int)duration.TotalSeconds;
                return seconds == 1 ? "1 second" : $"{seconds} seconds";
            }

            if (duration.TotalMinutes < 60)
            {
                int minutes = (int)duration.TotalMinutes;
                return minutes == 1 ? "1 minute" : $"{minutes} minutes";
            }

            int hours = (int)duration.TotalHours;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }
    }
}
