using Microsoft.Extensions.Logging;
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
        private const string UnknownValue = "Unknown";
        private const string UnknownApplicationVersion = "unknown";
        private const string UserAgentFallbackProduct = "Cotton-Mobile";

        private readonly CottonMobileOptions _options;
        private readonly ILogger<CottonMobileApplicationMetadata> _logger;

        public CottonMobileApplicationMetadata(
            CottonMobileOptions options,
            ILogger<CottonMobileApplicationMetadata> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options;
            _logger = logger;
        }

        public string ApplicationName => _options.ApplicationName;

        public string ApplicationVersion => ReadMetadata(
            "application version",
            () => AppInfo.Current.VersionString,
            UnknownApplicationVersion);

        public string ApplicationBuild => ReadMetadata(
            "application build",
            () => AppInfo.Current.BuildString,
            string.Empty);

        public string PackageName => ReadMetadata(
            "package name",
            () => AppInfo.Current.PackageName,
            string.Empty);

        public string InstallChannel => ResolveInstallChannel(PackageName);

        public string DeviceName => ReadMetadata("device name", CreateDeviceName, UnknownValue);

        public string OperatingSystem => ReadMetadata(
            "operating system",
            () => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}",
            UnknownValue);

        public string ScreenDetails => ReadMetadata(
            "screen details",
            () => CreateScreenDetails(DeviceDisplay.Current.MainDisplayInfo),
            UnknownValue);

        public string UserAgent =>
            $"{CreateUserAgentToken(ApplicationName, UserAgentFallbackProduct)}/{CreateUserAgentToken(ApplicationVersion, UnknownApplicationVersion)}";

        private string ReadMetadata(string name, Func<string> read, string fallback)
        {
            try
            {
                string value = read();
                return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Failed to read Cotton mobile {MetadataName}.", name);
                return fallback;
            }
        }

        private static string CreateDeviceName()
        {
            string deviceName = DeviceInfo.Current.Name;
            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                return deviceName;
            }

            return $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}";
        }

        private static string CreateScreenDetails(DisplayInfo displayInfo)
        {
            double density = displayInfo.Density <= 0 ? 1 : displayInfo.Density;
            double widthDp = displayInfo.Width / density;
            double heightDp = displayInfo.Height / density;
            return FormattableString.Invariant(
                $"{displayInfo.Width:0}x{displayInfo.Height:0}px · {widthDp:0}x{heightDp:0}dp · {density:0.##}x · {displayInfo.Orientation}");
        }

        private static string CreateUserAgentToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var buffer = new char[value.Length];
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                buffer[index] = IsHttpTokenCharacter(character) ? character : '-';
            }

            string token = new string(buffer).Trim('-');
            return string.IsNullOrWhiteSpace(token) ? fallback : token;
        }

        private static bool IsHttpTokenCharacter(char character)
        {
            return character is >= '0' and <= '9'
                or >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or '!'
                or '#'
                or '$'
                or '%'
                or '&'
                or '\''
                or '*'
                or '+'
                or '-'
                or '.'
                or '^'
                or '_'
                or '`'
                or '|'
                or '~';
        }

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
