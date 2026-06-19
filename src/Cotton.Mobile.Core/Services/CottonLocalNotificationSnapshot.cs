using System;

namespace Cotton.Mobile.Services
{
    public sealed class CottonLocalNotificationSnapshot
    {
        public CottonLocalNotificationSnapshot(
            int id,
            CottonLocalNotificationKind kind,
            CottonNotificationChannelKind channelKind,
            string title,
            string message)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Notification kind is not supported.");
            }

            if (!Enum.IsDefined(channelKind))
            {
                throw new ArgumentOutOfRangeException(nameof(channelKind), "Notification channel kind is not supported.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Notification title is required.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Notification message is required.", nameof(message));
            }

            Id = Math.Abs(id == int.MinValue ? 0 : id);
            Kind = kind;
            ChannelKind = channelKind;
            Title = title.Trim();
            Message = message.Trim();
        }

        public int Id { get; }

        public CottonLocalNotificationKind Kind { get; }

        public CottonNotificationChannelKind ChannelKind { get; }

        public string Title { get; }

        public string Message { get; }
    }
}
