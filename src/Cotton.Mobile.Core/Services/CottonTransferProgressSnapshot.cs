namespace Cotton.Mobile.Services
{
    public class CottonTransferProgressSnapshot
    {
        public CottonTransferProgressSnapshot(long transferredBytes, long? totalBytes)
        {
            if (transferredBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(transferredBytes), "Transferred bytes cannot be negative.");
            }

            if (totalBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalBytes), "Total bytes cannot be negative.");
            }

            if (totalBytes.HasValue && transferredBytes > totalBytes.Value)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(transferredBytes),
                    "Transferred bytes cannot exceed total bytes.");
            }

            TransferredBytes = transferredBytes;
            TotalBytes = totalBytes;
        }

        public long TransferredBytes { get; }

        public long? TotalBytes { get; }

        public int? Percent
        {
            get
            {
                if (TotalBytes is not > 0)
                {
                    return null;
                }

                return (int)Math.Min(100d, Math.Floor(TransferredBytes / (double)TotalBytes.Value * 100d));
            }
        }

        public string DisplayText
        {
            get
            {
                int? percent = Percent;
                if (percent.HasValue)
                {
                    return $"{percent}%";
                }

                return CottonFileSizeFormatter.Format(TransferredBytes);
            }
        }
    }
}
