namespace Cotton.Mobile.Services
{
    public static class CottonShareCaptureUploadName
    {
        public const string DuplicateErrorMessage = "Another captured file already uses that upload name.";

        public static string Create(CottonShareIntakeItemSnapshot item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return item.Type == CottonShareIntakeItemType.Text
                ? CottonShareTextUploadName.Create(item.UploadDisplayName ?? item.DisplayName)
                : item.EffectiveUploadDisplayName;
        }

        public static bool TryNormalizeRename(
            CottonShareIntakeItemSnapshot item,
            string? requestedName,
            IEnumerable<string> existingUploadNames,
            out string normalizedName,
            out string uploadName,
            out string errorMessage)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(existingUploadNames);

            uploadName = string.Empty;
            if (!CottonShareUploadNameValidator.TryNormalize(
                    requestedName,
                    [],
                    out normalizedName,
                    out errorMessage))
            {
                return false;
            }

            uploadName = Create(item, normalizedName);
            if (CottonCloudItemNameRules.ContainsDuplicateName(uploadName, existingUploadNames))
            {
                normalizedName = string.Empty;
                uploadName = string.Empty;
                errorMessage = DuplicateErrorMessage;
                return false;
            }

            return true;
        }

        private static string Create(CottonShareIntakeItemSnapshot item, string normalizedName)
        {
            return item.Type == CottonShareIntakeItemType.Text
                ? CottonShareTextUploadName.Create(normalizedName)
                : normalizedName;
        }
    }
}
