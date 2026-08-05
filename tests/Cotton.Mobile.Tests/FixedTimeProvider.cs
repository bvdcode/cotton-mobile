namespace Cotton.Mobile.Tests
{
    internal class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow =
            new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
