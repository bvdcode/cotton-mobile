namespace Cotton.Mobile.Services
{
    public class CottonTrashPermanentDeleteResult
    {
        private CottonTrashPermanentDeleteResult(
            Guid itemId,
            CottonFileBrowserEntryType itemType,
            string itemName)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Item id is required.", nameof(itemId));
            }

            if (itemType is not CottonFileBrowserEntryType.File and not CottonFileBrowserEntryType.Folder)
            {
                throw new ArgumentOutOfRangeException(nameof(itemType), "Delete item type is not supported.");
            }

            ItemId = itemId;
            ItemType = itemType;
            ItemName = string.IsNullOrWhiteSpace(itemName) ? "Item" : itemName.Trim();
        }

        public Guid ItemId { get; }

        public CottonFileBrowserEntryType ItemType { get; }

        public string ItemName { get; }

        public string StatusText => CottonTrashPermanentDeleteStatusText.CreateDeletedStatus(ItemName);

        public static CottonTrashPermanentDeleteResult Deleted(CottonFileBrowserEntry item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new CottonTrashPermanentDeleteResult(item.Id, item.Type, item.Name);
        }
    }
}
