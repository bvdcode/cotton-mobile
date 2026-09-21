// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public static class CottonSyncLocalFingerprint
    {
        public static string Create(CottonSyncRootSnapshot root, CottonDeviceToCloudLocalContentSnapshot content)
        {
            using MemoryStream buffer = new();
            using BinaryWriter writer = new(buffer, Encoding.UTF8, leaveOpen: true);
            writer.Write(root.StableKey);
            writer.Write(root.LocalRoot.ScopeKey ?? string.Empty);
            writer.Write(root.DeletesOriginalsAfterUpload);
            foreach (CottonDeviceToCloudLocalItemSnapshot item in content.Items.OrderBy(
                item => item.RelativePath, StringComparer.Ordinal))
            {
                writer.Write((int)item.ItemType);
                writer.Write(item.RelativePath);
                writer.Write(item.LocalSourceId ?? string.Empty);
                writer.Write(item.SizeBytes ?? -1);
                writer.Write(item.ContentHash ?? string.Empty);
                writer.Write(item.ContentType ?? string.Empty);
            }

            writer.Flush();
            return Convert.ToHexStringLower(SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)));
        }
    }
}
