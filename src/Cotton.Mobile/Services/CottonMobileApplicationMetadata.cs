// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace Cotton.Mobile.Services
{
    public class CottonMobileApplicationMetadata : ICottonMobileApplicationMetadata
    {
        private const string DebugApplicationIdSuffix = ".debug";
        private const string ReleaseApplicationId = "dev.cottoncloud.app";

        private readonly CottonMobileOptions _options;

        public CottonMobileApplicationMetadata(CottonMobileOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _options = options;
        }

        public string ApplicationName => _options.ApplicationName;

        public string ApplicationVersion => RequireMetadata(
            AppInfo.Current.VersionString,
            "Application version");

        public string ApplicationBuild => RequireMetadata(
            AppInfo.Current.BuildString,
            "Application build");

        public string PackageName => RequireMetadata(
            AppInfo.Current.PackageName,
            "Package name");

        public string InstallChannel => ResolveInstallChannel(PackageName);

        public string DeviceName => RequireMetadata(DeviceInfo.Current.Name, "Device name");

        public string OperatingSystem => RequireMetadata(
            $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}",
            "Operating system");

        public string ScreenDetails => CreateScreenDetails(DeviceDisplay.Current.MainDisplayInfo);

        public string UserAgent =>
            $"{CreateUserAgentToken(ApplicationName)}/{CreateUserAgentToken(ApplicationVersion)}";

        private static string RequireMetadata(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{name} is unavailable.");
            }

            return value.Trim();
        }

        private static string CreateScreenDetails(DisplayInfo displayInfo)
        {
            if (displayInfo.Density <= 0)
            {
                throw new InvalidOperationException("Display density must be positive.");
            }

            double density = displayInfo.Density;
            double widthDp = displayInfo.Width / density;
            double heightDp = displayInfo.Height / density;
            return FormattableString.Invariant(
                $"{displayInfo.Width:0}x{displayInfo.Height:0}px · {widthDp:0}x{heightDp:0}dp · {density:0.##}x · {displayInfo.Orientation}");
        }

        private static string CreateUserAgentToken(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);

            char[] buffer = new char[value.Length];
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                buffer[index] = IsHttpTokenCharacter(character) ? character : '-';
            }

            string token = new string(buffer).Trim('-');
            return string.IsNullOrWhiteSpace(token)
                ? throw new InvalidOperationException("Application metadata cannot form an HTTP product token.")
                : token;
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
            if (packageName.EndsWith(DebugApplicationIdSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return AppResources.DebugInstallChannel;
            }

            if (string.Equals(packageName, ReleaseApplicationId, StringComparison.OrdinalIgnoreCase))
            {
                return AppResources.ReleaseInstallChannel;
            }

            return AppResources.CustomInstallChannel;
        }
    }
}
