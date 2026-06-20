namespace Cotton.Mobile.Services
{
    public class CottonCloudStorageQuotaSnapshot
    {
        private const double NearLimitRatio = 0.9d;

        private CottonCloudStorageQuotaSnapshot(
            CottonCloudStorageQuotaStatus status,
            long? usedBytes,
            long? limitBytes,
            string summaryText,
            string detailText,
            double usageFraction)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Cloud storage quota status is unknown.");
            }

            if (usedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usedBytes), "Cloud storage used size cannot be negative.");
            }

            if (limitBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limitBytes), "Cloud storage quota limit cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(summaryText))
            {
                throw new ArgumentException("Cloud storage quota summary is required.", nameof(summaryText));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Cloud storage quota detail is required.", nameof(detailText));
            }

            Status = status;
            UsedBytes = usedBytes;
            LimitBytes = limitBytes;
            SummaryText = summaryText.Trim();
            DetailText = detailText.Trim();
            UsageFraction = Math.Clamp(usageFraction, 0d, 1d);
        }

        public string Title => "Account storage";

        public CottonCloudStorageQuotaStatus Status { get; }

        public long? UsedBytes { get; }

        public long? LimitBytes { get; }

        public string SummaryText { get; }

        public string DetailText { get; }

        public double UsageFraction { get; }

        public bool IsProgressVisible =>
            Status is CottonCloudStorageQuotaStatus.WithinLimit
                or CottonCloudStorageQuotaStatus.NearLimit
                or CottonCloudStorageQuotaStatus.OverLimit;

        public bool IsAttentionVisible =>
            Status is CottonCloudStorageQuotaStatus.NearLimit
                or CottonCloudStorageQuotaStatus.OverLimit;

        public static CottonCloudStorageQuotaSnapshot Unavailable { get; } = new(
            CottonCloudStorageQuotaStatus.Unavailable,
            usedBytes: null,
            limitBytes: null,
            "Storage limit unavailable.",
            "Storage limits are not shared by this server.",
            usageFraction: 0d);

        public static CottonCloudStorageQuotaSnapshot Unknown { get; } = new(
            CottonCloudStorageQuotaStatus.Unknown,
            usedBytes: null,
            limitBytes: null,
            "Account storage not checked.",
            "Refresh storage to check account usage.",
            usageFraction: 0d);

        public static CottonCloudStorageQuotaSnapshot Create(long usedBytes, long? limitBytes)
        {
            if (usedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usedBytes), "Cloud storage used size cannot be negative.");
            }

            if (limitBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limitBytes), "Cloud storage quota limit cannot be negative.");
            }

            if (!limitBytes.HasValue)
            {
                return new CottonCloudStorageQuotaSnapshot(
                    CottonCloudStorageQuotaStatus.Unlimited,
                    usedBytes,
                    limitBytes,
                    $"{FormatSize(usedBytes)} used",
                    "No account quota reported.",
                    usageFraction: 0d);
            }

            long limit = limitBytes.Value;
            long remainingBytes = limit - usedBytes;
            double usageFraction = limit == 0
                ? usedBytes == 0 ? 0d : 1d
                : (double)usedBytes / limit;
            CottonCloudStorageQuotaStatus status = ResolveStatus(usedBytes, limit);
            string detailText = remainingBytes >= 0
                ? $"{FormatSize(remainingBytes)} available"
                : $"{FormatSize(Math.Abs(remainingBytes))} over account quota";

            return new CottonCloudStorageQuotaSnapshot(
                status,
                usedBytes,
                limit,
                $"{FormatSize(usedBytes)} of {FormatSize(limit)} used",
                detailText,
                usageFraction);
        }

        private static CottonCloudStorageQuotaStatus ResolveStatus(long usedBytes, long limitBytes)
        {
            if (usedBytes > limitBytes)
            {
                return CottonCloudStorageQuotaStatus.OverLimit;
            }

            if (limitBytes > 0 && usedBytes >= limitBytes * NearLimitRatio)
            {
                return CottonCloudStorageQuotaStatus.NearLimit;
            }

            return CottonCloudStorageQuotaStatus.WithinLimit;
        }

        private static string FormatSize(long sizeBytes)
        {
            return CottonFileSizeFormatter.Format(sizeBytes);
        }
    }
}
