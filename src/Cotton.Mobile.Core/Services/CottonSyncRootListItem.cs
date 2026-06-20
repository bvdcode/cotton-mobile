namespace Cotton.Mobile.Services
{
    public class CottonSyncRootListItem
    {
        public CottonSyncRootListItem(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            Id = root.Id;
            Title = root.CloudFolder.FolderName;
            PathText = root.CloudFolder.Path;
            DetailText = $"{CreateDirectionText(root.Direction)} · {root.LocalRoot.DisplayName}";
            StatusText = root.StatusText;
            IsReady = root.CanRunSync;
            IsAttentionVisible = root.NeedsUserAction || !root.CanRunSync;
        }

        public Guid Id { get; }

        public string Title { get; }

        public string PathText { get; }

        public string DetailText { get; }

        public string StatusText { get; }

        public bool IsReady { get; }

        public bool IsAttentionVisible { get; }

        private static string CreateDirectionText(CottonSyncDirection direction)
        {
            return direction switch
            {
                CottonSyncDirection.CloudToDevice => "Cloud to device",
                CottonSyncDirection.DeviceToCloud => "Device to cloud",
                CottonSyncDirection.Bidirectional => "Bidirectional",
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported."),
            };
        }
    }
}
