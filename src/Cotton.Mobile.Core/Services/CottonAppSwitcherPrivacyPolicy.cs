namespace Cotton.Mobile.Services
{
    public class CottonAppSwitcherPrivacyPolicy
    {
        public bool ShouldHidePreviews(
            CottonAppLockSettings settings,
            CottonAppLockCapabilitySnapshot capability)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(capability);

            return settings.IsEnabled && capability.CanEnable;
        }
    }
}
