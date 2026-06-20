namespace Cotton.Mobile.Services
{
    public class CottonRemotePushPreferences
    {
        public static CottonRemotePushPreferences Default { get; } =
            new CottonRemotePushPreferences(
                sharedFile: false,
                accessRequest: false,
                commentMention: false,
                securitySession: true);

        public CottonRemotePushPreferences(
            bool sharedFile,
            bool accessRequest,
            bool commentMention,
            bool securitySession)
        {
            SharedFile = sharedFile;
            AccessRequest = accessRequest;
            CommentMention = commentMention;
            SecuritySession = securitySession;
        }

        public bool SharedFile { get; }

        public bool AccessRequest { get; }

        public bool CommentMention { get; }

        public bool SecuritySession { get; }

        public int EnabledCategoryCount =>
            (SharedFile ? 1 : 0)
            + (AccessRequest ? 1 : 0)
            + (CommentMention ? 1 : 0)
            + (SecuritySession ? 1 : 0);

        public bool IsEnabled(CottonRemotePushEventCategory category)
        {
            return category switch
            {
                CottonRemotePushEventCategory.SharedFile => SharedFile,
                CottonRemotePushEventCategory.AccessRequest => AccessRequest,
                CottonRemotePushEventCategory.CommentMention => CommentMention,
                CottonRemotePushEventCategory.SecuritySession => SecuritySession,
                _ => false,
            };
        }

        public CottonRemotePushPreferences WithCategory(
            CottonRemotePushEventCategory category,
            bool isEnabled)
        {
            return category switch
            {
                CottonRemotePushEventCategory.SharedFile => new CottonRemotePushPreferences(
                    isEnabled,
                    AccessRequest,
                    CommentMention,
                    SecuritySession),
                CottonRemotePushEventCategory.AccessRequest => new CottonRemotePushPreferences(
                    SharedFile,
                    isEnabled,
                    CommentMention,
                    SecuritySession),
                CottonRemotePushEventCategory.CommentMention => new CottonRemotePushPreferences(
                    SharedFile,
                    AccessRequest,
                    isEnabled,
                    SecuritySession),
                CottonRemotePushEventCategory.SecuritySession => new CottonRemotePushPreferences(
                    SharedFile,
                    AccessRequest,
                    CommentMention,
                    isEnabled),
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown remote push category."),
            };
        }
    }
}
