namespace Cotton.Mobile.Services
{
    public class CottonOfflineFolderTreeFileSnapshot
    {
        public CottonOfflineFolderTreeFileSnapshot(
            CottonFileBrowserEntry file,
            string displayPath)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Offline folder tree file snapshots require file entries.", nameof(file));
            }

            if (string.IsNullOrWhiteSpace(displayPath))
            {
                throw new ArgumentException("Display path is required.", nameof(displayPath));
            }

            File = file;
            DisplayPath = displayPath.Trim();
        }

        public CottonFileBrowserEntry File { get; }

        public string DisplayPath { get; }
    }
}
