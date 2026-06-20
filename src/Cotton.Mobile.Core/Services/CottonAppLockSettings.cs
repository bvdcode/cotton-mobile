namespace Cotton.Mobile.Services
{
    public class CottonAppLockSettings
    {
        public CottonAppLockSettings(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        public static CottonAppLockSettings Disabled { get; } = new(false);

        public bool IsEnabled { get; }

        public CottonAppLockSettings WithEnabled(bool isEnabled)
        {
            return IsEnabled == isEnabled ? this : new CottonAppLockSettings(isEnabled);
        }
    }
}
