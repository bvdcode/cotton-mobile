using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class StubCottonInstanceProbe : ICottonInstanceProbe
    {
        private readonly Func<Uri, bool> _result;

        public StubCottonInstanceProbe(Func<Uri, bool> result)
        {
            ArgumentNullException.ThrowIfNull(result);
            _result = result;
        }

        public List<Uri> ProbedUris { get; } = [];

        public Task<bool> IsCottonInstanceAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProbedUris.Add(instanceUri);
            return Task.FromResult(_result(instanceUri));
        }
    }
}
