// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System;

namespace Cotton.Mobile.Services
{
    public sealed class CottonNotificationChannelSnapshot
    {
        public CottonNotificationChannelSnapshot(
            CottonNotificationChannelKind kind,
            string id,
            string name,
            string description,
            CottonNotificationImportance importance,
            bool defaultEnabled)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    "Notification channel kind is not supported.");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Notification channel id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Notification channel name is required.", nameof(name));
            }

            if (!Enum.IsDefined(importance))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(importance),
                    "Notification importance is not supported.");
            }

            Kind = kind;
            Id = id.Trim();
            Name = name.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? Name : description.Trim();
            Importance = importance;
            DefaultEnabled = defaultEnabled;
        }

        public CottonNotificationChannelKind Kind { get; }

        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public CottonNotificationImportance Importance { get; }

        public bool DefaultEnabled { get; }
    }
}
