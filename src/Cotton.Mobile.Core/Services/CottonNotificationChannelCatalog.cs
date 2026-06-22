// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Cotton.Mobile.Services
{
    public static class CottonNotificationChannelCatalog
    {
        private static readonly CottonNotificationChannelSnapshot[] Channels =
        {
            new CottonNotificationChannelSnapshot(
                CottonNotificationChannelKind.Transfers,
                "cotton.transfers",
                "Transfers",
                "Upload and download status.",
                CottonNotificationImportance.Default,
                defaultEnabled: true),
            new CottonNotificationChannelSnapshot(
                CottonNotificationChannelKind.Backup,
                "cotton.backup",
                "Backup",
                "Camera backup status and blocked backup alerts.",
                CottonNotificationImportance.Default,
                defaultEnabled: true),
            new CottonNotificationChannelSnapshot(
                CottonNotificationChannelKind.Shares,
                "cotton.shares",
                "Shares",
                "Incoming share and capture inbox status.",
                CottonNotificationImportance.Low,
                defaultEnabled: false),
            new CottonNotificationChannelSnapshot(
                CottonNotificationChannelKind.Security,
                "cotton.security",
                "Security",
                "Session and account security alerts.",
                CottonNotificationImportance.High,
                defaultEnabled: true),
        };

        public static IReadOnlyList<CottonNotificationChannelSnapshot> All => Channels;

        public static CottonNotificationChannelSnapshot Get(CottonNotificationChannelKind kind)
        {
            return Channels.FirstOrDefault(channel => channel.Kind == kind)
                ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown notification channel kind.");
        }
    }
}
