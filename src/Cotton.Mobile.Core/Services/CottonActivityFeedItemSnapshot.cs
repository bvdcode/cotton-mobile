namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedItemSnapshot
    {
        public CottonActivityFeedItemSnapshot(
            Guid id,
            string title,
            string? content,
            DateTime createdAt,
            DateTime? readAt,
            CottonActivityFeedPriority priority,
            IReadOnlyDictionary<string, string>? metadata)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Activity feed item id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Activity feed item title is required.", nameof(title));
            }

            if (!Enum.IsDefined(priority))
            {
                throw new ArgumentOutOfRangeException(nameof(priority), "Activity feed priority is not supported.");
            }

            Id = id;
            Title = title.Trim();
            Content = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
            CreatedAt = createdAt;
            ReadAt = readAt;
            Priority = priority;
            Metadata = metadata is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(metadata);
        }

        public Guid Id { get; }

        public string Title { get; }

        public string? Content { get; }

        public DateTime CreatedAt { get; }

        public DateTime? ReadAt { get; }

        public CottonActivityFeedPriority Priority { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }

        public bool IsUnread => !ReadAt.HasValue;
    }
}
