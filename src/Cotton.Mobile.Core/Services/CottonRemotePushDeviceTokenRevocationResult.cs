namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDeviceTokenRevocationResult
    {
        public CottonRemotePushDeviceTokenRevocationResult(int revokedTokens)
        {
            if (revokedTokens < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revokedTokens));
            }

            RevokedTokens = revokedTokens;
        }

        public int RevokedTokens { get; }
    }
}
