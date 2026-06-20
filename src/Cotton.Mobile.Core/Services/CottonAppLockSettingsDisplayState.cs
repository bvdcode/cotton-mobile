namespace Cotton.Mobile.Services
{
    public class CottonAppLockSettingsDisplayState
    {
        private CottonAppLockSettingsDisplayState(
            CottonAppLockSettings settings,
            CottonAppLockCapabilitySnapshot capability)
        {
            Settings = settings;
            Capability = capability;
            IsEnabled = settings.IsEnabled && capability.CanEnable;
            CanToggle = capability.CanEnable;
            StatusText = capability.CanEnable
                ? IsEnabled ? "On" : "Off"
                : capability.StatusText;
            DetailText = capability.CanEnable
                ? "Require device unlock before opening Cotton."
                : capability.DetailText;
        }

        public CottonAppLockSettings Settings { get; }

        public CottonAppLockCapabilitySnapshot Capability { get; }

        public string Title => "App lock";

        public bool IsEnabled { get; }

        public bool CanToggle { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public static CottonAppLockSettingsDisplayState Create(
            CottonAppLockSettings settings,
            CottonAppLockCapabilitySnapshot capability)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(capability);

            return new CottonAppLockSettingsDisplayState(settings, capability);
        }
    }
}
