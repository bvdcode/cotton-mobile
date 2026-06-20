namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushSessionRegistrationStatusProvider
    {
        CottonRemotePushRegistrationStatus? LastRegistrationStatus { get; }

        DateTimeOffset? LastRegistrationAttemptedAtUtc { get; }
    }
}
