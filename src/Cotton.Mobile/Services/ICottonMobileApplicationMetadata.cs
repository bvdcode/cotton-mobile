namespace Cotton.Mobile.Services
{
    public interface ICottonMobileApplicationMetadata
    {
        string ApplicationName { get; }

        string ApplicationVersion { get; }

        string ApplicationBuild { get; }

        string PackageName { get; }

        string InstallChannel { get; }

        string DeviceName { get; }

        string OperatingSystem { get; }

        string UserAgent { get; }
    }
}
