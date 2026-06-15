using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace Cotton.Mobile.Services
{
    public class CottonMobileApplicationMetadata : ICottonMobileApplicationMetadata
    {
        private const string DefaultApplicationName = "Cotton Cloud";

        public string ApplicationName => DefaultApplicationName;

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

        public string UserAgent => $"{ApplicationName}/{ApplicationVersion}";
    }
}
