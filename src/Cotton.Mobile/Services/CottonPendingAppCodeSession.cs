namespace Cotton.Mobile.Services
{
    public class CottonPendingAppCodeSession
    {
        public Uri InstanceUri { get; set; } = new("about:blank");

        public Guid ApprovalId { get; set; }

        public Uri ApprovalUri { get; set; } = new("about:blank");

        public string PollToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public TimeSpan PollInterval { get; set; }
    }
}
