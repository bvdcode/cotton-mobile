namespace Cotton.Mobile.Services
{
    public static class CottonCloudItemNameRules
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

        public static bool IsReservedPathSegment(string name)
        {
            return name is "." or "..";
        }

        public static bool ContainsInvalidCharacter(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return name.IndexOfAny(InvalidCharacters) >= 0;
        }

        public static bool ContainsDuplicateName(string name, IEnumerable<string> existingNames)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(existingNames);

            return existingNames.Any(existingName => string.Equals(
                name,
                existingName.Trim(),
                StringComparison.OrdinalIgnoreCase));
        }
    }
}
