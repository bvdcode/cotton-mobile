namespace Cotton.Mobile.Services
{
    public class CottonFileUploadProgressSnapshot
    {
        public CottonFileUploadProgressSnapshot(CottonFileUploadSourceSnapshot source, long uploadedBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (uploadedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(uploadedBytes), "Uploaded bytes cannot be negative.");
            }

            Source = source;
            UploadedBytes = uploadedBytes;
        }

        public CottonFileUploadSourceSnapshot Source { get; }

        public long UploadedBytes { get; }

        public int? Percent
        {
            get
            {
                if (Source.SizeBytes is not > 0)
                {
                    return null;
                }

                return (int)Math.Min(100d, Math.Floor(UploadedBytes / (double)Source.SizeBytes.Value * 100d));
            }
        }

        public string StatusText
        {
            get
            {
                int? percent = Percent;
                return percent.HasValue
                    ? $"Uploading {Source.Name}... {percent}%"
                    : $"Uploading {Source.Name}... {CottonFileSizeFormatter.Format(UploadedBytes)}";
            }
        }
    }
}
