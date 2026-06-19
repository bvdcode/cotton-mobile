#if ANDROID
using Android.OS;

namespace Cotton.Mobile.Services
{
    public sealed class AndroidApiLevelProvider : IAndroidApiLevelProvider
    {
        public int CurrentApiLevel => (int)Build.VERSION.SdkInt;
    }
}
#endif
