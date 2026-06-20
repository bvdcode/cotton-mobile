namespace Cotton.Mobile.Services
{
    public static class CottonAccountScopeKey
    {
        private const string UserPrefix = "user:";

        public static bool TryCreateFromUsername(string? username, out string accountScopeKey)
        {
            accountScopeKey = string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            accountScopeKey = UserPrefix + username.Trim();
            return true;
        }
    }
}
