using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace Cotton.Mobile.Services
{
    public class CottonMobileApplicationMetadata : ICottonMobileApplicationMetadata
    {
        private const string DebugApplicationIdSuffix = ".debug";
        private const string ReleaseApplicationId = "dev.cottoncloud.app";
        private const string UnknownInstallChannel = "Unknown";
        private const string DebugInstallChannel = "Debug APK";
        private const string ReleaseInstallChannel = "Release package";
        private const string CustomInstallChannel = "Custom package";

        private readonly CottonMobileOptions _options;

        public CottonMobileApplicationMetadata(CottonMobileOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
        }

        public string ApplicationName => _options.ApplicationName;

        public string ApplicationVersion => AppInfo.Current.VersionString;

        public string ApplicationBuild => AppInfo.Current.BuildString;

        public string PackageName => AppInfo.Current.PackageName;

        public string InstallChannel => ResolveInstallChannel(PackageName);

        public string DeviceName
        {
            get
            {
                string deviceName = DeviceInfo.Current.Name;
                if (!string.IsNullOrWhiteSpace(deviceName))
                {
                    return deviceName.Trim();
                }

                return $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
            }
        }

        public string OperatingSystem => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}".Trim();

        public string UserAgent => $"{ApplicationName}/{ApplicationVersion}";

        private static string ResolveInstallChannel(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return UnknownInstallChannel;
            }

            if (packageName.EndsWith(DebugApplicationIdSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return DebugInstallChannel;
            }

            if (string.Equals(packageName, ReleaseApplicationId, StringComparison.OrdinalIgnoreCase))
            {
                return ReleaseInstallChannel;
            }

            return CustomInstallChannel;
        }
    }
}
