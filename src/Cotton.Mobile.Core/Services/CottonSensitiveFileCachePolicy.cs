// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSensitiveFileCachePolicy
    {
        private static readonly HashSet<string> SensitiveFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".age",
            ".asc",
            ".cer",
            ".crt",
            ".csr",
            ".der",
            ".gpg",
            ".jks",
            ".kdbx",
            ".key",
            ".keystore",
            ".p12",
            ".p8",
            ".pem",
            ".pfx",
            ".pgp",
        };

        private static readonly HashSet<string> SensitiveFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dockercfg",
            ".env",
            ".netrc",
            ".npmrc",
            ".pgpass",
            ".pypirc",
            "credentials",
            "credentials.json",
            "id_dsa",
            "id_ecdsa",
            "id_ed25519",
            "id_rsa",
            "secrets.json",
        };

        private static readonly HashSet<string> SensitiveContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pgp-encrypted",
            "application/pgp-keys",
            "application/pgp-signature",
            "application/pkcs10",
            "application/pkcs8",
            "application/pkcs12",
            "application/pkix-cert",
            "application/pkix-crl",
            "application/vnd.keepass",
            "application/x-keepass2",
            "application/x-pem-file",
            "application/x-pkcs12",
        };

        private static readonly HashSet<string> SensitiveNameParts = new(StringComparer.OrdinalIgnoreCase)
        {
            "credential",
            "credentials",
            "passwd",
            "password",
            "passwords",
            "secret",
            "secrets",
            "token",
            "tokens",
        };

        private static readonly string[] SensitiveNamePhrases =
        [
            "api-key",
            "api_key",
            "private-key",
            "private_key",
        ];

        public static bool IsSensitiveFile(CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return file.Type == CottonFileBrowserEntryType.File
                && IsSensitiveFile(file.Name, file.ContentType);
        }

        public static bool IsSensitiveFile(string? fileName, string? contentType)
        {
            string mediaType = CottonFileKindClassifier.CreateContentTypeMediaType(contentType);
            if (SensitiveContentTypes.Contains(mediaType))
            {
                return true;
            }

            string normalizedName = NormalizeFileName(fileName);
            if (normalizedName.Length == 0)
            {
                return false;
            }

            return SensitiveFileNames.Contains(normalizedName)
                || normalizedName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase)
                || SensitiveFileExtensions.Contains(Path.GetExtension(normalizedName))
                || HasSensitiveNamePhrase(normalizedName)
                || HasSensitiveNamePart(normalizedName);
        }

        public static bool CanReuseUnpinnedLocalCopy(CottonFileBrowserEntry file)
        {
            return !IsSensitiveFile(file);
        }

        public static bool RequiresSensitiveCacheEviction(CottonFileDownloadCacheEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return entry.RequiresSensitiveEviction;
        }

        private static string NormalizeFileName(string? fileName)
        {
            return string.IsNullOrWhiteSpace(fileName)
                ? string.Empty
                : Path.GetFileName(fileName.Trim());
        }

        private static bool HasSensitiveNamePhrase(string normalizedName)
        {
            return SensitiveNamePhrases.Any(
                phrase => normalizedName.Contains(phrase, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasSensitiveNamePart(string normalizedName)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedName);
            foreach (string part in SplitNameParts(nameWithoutExtension))
            {
                if (SensitiveNameParts.Contains(part))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> SplitNameParts(string value)
        {
            return value.Split(
                [' ', '.', '-', '_', '(', ')', '[', ']'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
