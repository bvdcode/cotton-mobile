using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Tests
{
    internal class TestSecureStorage : ISecureStorage
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
        public List<string> Reads { get; } = [];
        public List<string> Writes { get; } = [];
        public List<string> Removes { get; } = [];
        public Func<Task>? BeforeWrite { get; set; }
        public Func<Task>? AfterWrite { get; set; }
        public bool FailRead { get; set; }
        public bool FailWrite { get; set; }
        public string? FailedRemovalKey { get; set; }

        public Task<string?> GetAsync(string key)
        {
            Reads.Add(key);
            if (FailRead)
            {
                throw new IOException("Read failed.");
            }

            Values.TryGetValue(key, out string? value);
            return Task.FromResult(value);
        }

        public async Task SetAsync(string key, string value)
        {
            if (BeforeWrite is not null)
            {
                await BeforeWrite();
            }

            if (FailWrite)
            {
                throw new IOException("Write failed.");
            }

            Values[key] = value;
            Writes.Add(key);
            if (AfterWrite is not null)
            {
                await AfterWrite();
            }
        }

        public bool Remove(string key)
        {
            Removes.Add(key);
            if (key == FailedRemovalKey)
            {
                throw new IOException("Removal failed.");
            }

            return Values.Remove(key);
        }

        public void RemoveAll()
        {
            throw new NotSupportedException("Only owned keys may be removed.");
        }
    }
}
