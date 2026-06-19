namespace Cotton.Mobile.Services
{
    public static class CottonShareUploadNameValidator
    {
        private static readonly char[] InvalidCharacters =
        [
            '/',
            '\\',
            ':',
            '*',
            '?',
            '"',
            '<',
            '>',
            '|',
        ];

        public static bool TryNormalize(
            string? value,
            IEnumerable<string> existingNames,
            out string normalizedName,
            out string errorMessage)
        {
            ArgumentNullException.ThrowIfNull(existingNames);

            normalizedName = string.Empty;
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = "Enter a file name.";
                return false;
            }

            string name = value.Trim();
            if (name is "." or "..")
            {
                errorMessage = "Use a file name, not a folder path.";
                return false;
            }

            if (name.IndexOfAny(InvalidCharacters) >= 0)
            {
                errorMessage = "File names cannot contain path separators or reserved characters.";
                return false;
            }

            if (existingNames.Any(existingName => string.Equals(
                    name,
                    existingName.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "Another captured file already uses that upload name.";
                return false;
            }

            normalizedName = name;
            return true;
        }
    }
}
