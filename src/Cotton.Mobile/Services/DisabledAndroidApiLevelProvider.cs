namespace Cotton.Mobile.Services
{
    public sealed class DisabledAndroidApiLevelProvider : IAndroidApiLevelProvider
    {
        public int CurrentApiLevel => 0;
    }
}
