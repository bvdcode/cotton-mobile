namespace Cotton.Mobile.Services
{
    public static class CottonFileUploadNameResolver
    {
        public static string ResolveUniqueName(string? desiredName, IEnumerable<string> existingNames)
        {
            ArgumentNullException.ThrowIfNull(existingNames);

            string normalizedName = new CottonFileUploadSourceSnapshot(
                desiredName,
                CottonFileUploadSourceSnapshot.DefaultContentType,
                null).Name;
            var usedNames = new HashSet<string>(
                existingNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            if (!usedNames.Contains(normalizedName))
            {
                return normalizedName;
            }

            string extension = Path.GetExtension(normalizedName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedName);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                nameWithoutExtension = CottonFileUploadSourceSnapshot.DefaultFileName;
            }

            for (int suffix = 1; suffix < int.MaxValue; suffix++)
            {
                string candidate = $"{nameWithoutExtension} ({suffix}){extension}";
                if (!usedNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Could not resolve a unique upload file name.");
        }
    }
}
