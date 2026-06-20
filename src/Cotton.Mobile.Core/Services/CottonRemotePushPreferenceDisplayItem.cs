namespace Cotton.Mobile.Services
{
    public class CottonRemotePushPreferenceDisplayItem
    {
        public CottonRemotePushPreferenceDisplayItem(
            CottonRemotePushEventCategory category,
            string title,
            string detailText,
            bool isEnabled)
        {
            if (!Enum.IsDefined(category))
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Detail text is required.", nameof(detailText));
            }

            Category = category;
            Title = title;
            DetailText = detailText;
            IsEnabled = isEnabled;
        }

        public CottonRemotePushEventCategory Category { get; }

        public string Title { get; }

        public string DetailText { get; }

        public bool IsEnabled { get; }
    }
}
