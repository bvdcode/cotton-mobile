namespace Cotton.Mobile.Services
{
    public class CottonFilesShellNavigationItem
    {
        public CottonFilesShellNavigationItem(
            CottonFilesShellNavigationDestination destination,
            string label,
            string accessibilityText)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Navigation label is required.", nameof(label));
            }

            if (string.IsNullOrWhiteSpace(accessibilityText))
            {
                throw new ArgumentException("Navigation accessibility text is required.", nameof(accessibilityText));
            }

            Destination = destination;
            Label = label;
            AccessibilityText = accessibilityText;
        }

        public CottonFilesShellNavigationDestination Destination { get; }

        public string Label { get; }

        public string AccessibilityText { get; }
    }
}
