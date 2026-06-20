namespace Cotton.Mobile.Services
{
    public class CottonSyncConflictActionSnapshot
    {
        public CottonSyncConflictActionSnapshot(
            CottonSyncConflictResolutionAction action,
            string text,
            bool isPrimary,
            bool isDestructive)
        {
            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), "Sync conflict action is not supported.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Sync conflict action text is required.", nameof(text));
            }

            Action = action;
            Text = text.Trim();
            IsPrimary = isPrimary;
            IsDestructive = isDestructive;
        }

        public CottonSyncConflictResolutionAction Action { get; }

        public string Text { get; }

        public bool IsPrimary { get; }

        public bool IsDestructive { get; }
    }
}
