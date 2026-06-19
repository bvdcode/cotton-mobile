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
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Notification channel id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Notification channel name is required.", nameof(name));
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
