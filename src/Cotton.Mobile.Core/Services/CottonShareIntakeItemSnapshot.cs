namespace Cotton.Mobile.Services
{
    public class CottonShareIntakeItemSnapshot
    {
        public CottonShareIntakeItemSnapshot(
            Guid id,
            CottonShareIntakeItemType type,
            string value,
            string? displayName,
            string? mimeType)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Share intake item id cannot be empty.", nameof(id));
            }

            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Share intake item value cannot be empty.", nameof(value));
            }

            Id = id;
            Type = type;
            Value = value.Trim();
            DisplayName = NormalizeOptional(displayName);
            MimeType = NormalizeOptional(mimeType);
        }

        public Guid Id { get; }

        public CottonShareIntakeItemType Type { get; }

        public string Value { get; }

        public string? DisplayName { get; }

        public string? MimeType { get; }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
