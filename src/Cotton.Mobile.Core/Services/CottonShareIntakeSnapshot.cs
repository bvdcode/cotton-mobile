namespace Cotton.Mobile.Services
{
    public class CottonShareIntakeSnapshot
    {
        public CottonShareIntakeSnapshot(
            Guid id,
            CottonShareIntakeKind kind,
            CottonShareIntakeStatus status,
            string? sourceMimeType,
            IReadOnlyList<CottonShareIntakeItemSnapshot> items,
            string? failureMessage,
            DateTime receivedAtUtc)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Share intake id cannot be empty.", nameof(id));
            }

            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            ArgumentNullException.ThrowIfNull(items);
            if (items.Count == 0)
            {
                throw new ArgumentException("Share intake must contain at least one item.", nameof(items));
            }

            if (receivedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Share intake received timestamp must be UTC.", nameof(receivedAtUtc));
            }

            Id = id;
            Kind = kind;
            Status = status;
            SourceMimeType = NormalizeOptional(sourceMimeType);
            Items = items.ToList();
            FailureMessage = NormalizeOptional(failureMessage);
            ReceivedAtUtc = receivedAtUtc;
        }

        public Guid Id { get; }

        public CottonShareIntakeKind Kind { get; }

        public CottonShareIntakeStatus Status { get; }

        public string? SourceMimeType { get; }

        public IReadOnlyList<CottonShareIntakeItemSnapshot> Items { get; }

        public string? FailureMessage { get; }

        public DateTime ReceivedAtUtc { get; }

        public int ItemCount => Items.Count;

        public bool CanStageForCaptureInbox => Status == CottonShareIntakeStatus.Pending;

        public static CottonShareIntakeSnapshot CreatePending(
            Guid id,
            CottonShareIntakeKind kind,
            string? sourceMimeType,
            IReadOnlyList<CottonShareIntakeItemSnapshot> items,
            DateTime receivedAtUtc)
        {
            return new CottonShareIntakeSnapshot(
                id,
                kind,
                CottonShareIntakeStatus.Pending,
                sourceMimeType,
                items,
                failureMessage: null,
                receivedAtUtc);
        }

        public static CottonShareIntakeSnapshot CreateProblem(
            Guid id,
            CottonShareIntakeKind kind,
            CottonShareIntakeStatus status,
            string? sourceMimeType,
            IReadOnlyList<CottonShareIntakeItemSnapshot> items,
            string failureMessage,
            DateTime receivedAtUtc)
        {
            if (status == CottonShareIntakeStatus.Pending)
            {
                throw new ArgumentException("Use CreatePending for pending share intake snapshots.", nameof(status));
            }

            return new CottonShareIntakeSnapshot(
                id,
                kind,
                status,
                sourceMimeType,
                items,
                failureMessage,
                receivedAtUtc);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
