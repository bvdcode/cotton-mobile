namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkCapabilitySnapshot
    {
        public static CottonCloudShareLinkCapabilitySnapshot Current { get; } =
            new(
                supportsFileLinks: true,
                supportsFolderLinks: true,
                supportsLinkExpiration: true,
                supportsGlobalInvalidate: true,
                supportsPerLinkRevoke: false,
                supportsPassword: false,
                supportsActivityFeed: false,
                supportsSharedWithMe: false,
                supportsAccessRequests: false);

        public CottonCloudShareLinkCapabilitySnapshot(
            bool supportsFileLinks,
            bool supportsFolderLinks,
            bool supportsLinkExpiration,
            bool supportsGlobalInvalidate,
            bool supportsPerLinkRevoke,
            bool supportsPassword,
            bool supportsActivityFeed,
            bool supportsSharedWithMe,
            bool supportsAccessRequests)
        {
            SupportsFileLinks = supportsFileLinks;
            SupportsFolderLinks = supportsFolderLinks;
            SupportsLinkExpiration = supportsLinkExpiration;
            SupportsGlobalInvalidate = supportsGlobalInvalidate;
            SupportsPerLinkRevoke = supportsPerLinkRevoke;
            SupportsPassword = supportsPassword;
            SupportsActivityFeed = supportsActivityFeed;
            SupportsSharedWithMe = supportsSharedWithMe;
            SupportsAccessRequests = supportsAccessRequests;
        }

        public bool SupportsFileLinks { get; }

        public bool SupportsFolderLinks { get; }

        public bool SupportsLinkExpiration { get; }

        public bool SupportsGlobalInvalidate { get; }

        public bool SupportsPerLinkRevoke { get; }

        public bool SupportsPassword { get; }

        public bool SupportsActivityFeed { get; }

        public bool SupportsSharedWithMe { get; }

        public bool SupportsAccessRequests { get; }

        public bool CanCreateLink(CottonCloudShareLinkTargetKind targetKind)
        {
            return targetKind switch
            {
                CottonCloudShareLinkTargetKind.File => SupportsFileLinks,
                CottonCloudShareLinkTargetKind.Folder => SupportsFolderLinks,
                _ => false,
            };
        }
    }
}
