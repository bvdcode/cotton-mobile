using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace Cotton.Mobile.Services
{
    public class CottonMobileApplicationMetadata : ICottonMobileApplicationMetadata
    {
        private readonly CottonMobileOptions _options;

        public CottonMobileApplicationMetadata(CottonMobileOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
        }

        public string ApplicationName => _options.ApplicationName;

        public string ApplicationVersion => AppInfo.Current.VersionString;

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
    }
}
