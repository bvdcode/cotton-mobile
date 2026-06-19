namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkSnapshot
    {
        private CottonCloudShareLinkSnapshot(
            CottonCloudShareLinkTargetKind targetKind,
            Guid targetId,
            int expireAfterMinutes,
            string backendLink,
            string token,
            Uri shareUri)
        {
            TargetKind = targetKind;
            TargetId = targetId;
            ExpireAfterMinutes = expireAfterMinutes;
            BackendLink = backendLink;
            Token = token;
            ShareUri = shareUri;
            ShareUrl = shareUri.AbsoluteUri;
        }

        public CottonCloudShareLinkTargetKind TargetKind { get; }

        public Guid TargetId { get; }

        public int ExpireAfterMinutes { get; }

        public string BackendLink { get; }

        public string Token { get; }

        public Uri ShareUri { get; }

        public string ShareUrl { get; }

        public static CottonCloudShareLinkSnapshot Create(
            CottonCloudShareLinkRequest request,
            Uri instanceUri,
            string backendLink)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(backendLink);

            string token = CottonCloudShareLinkUrlBuilder.ExtractToken(backendLink)
                ?? throw new ArgumentException("Share link token was not found.", nameof(backendLink));
            Uri shareUri = CottonCloudShareLinkUrlBuilder.CreateShareUri(instanceUri, token);

            return new CottonCloudShareLinkSnapshot(
                request.TargetKind,
                request.TargetId,
                request.ExpireAfterMinutes,
                backendLink,
                token,
                shareUri);
        }
    }
}
