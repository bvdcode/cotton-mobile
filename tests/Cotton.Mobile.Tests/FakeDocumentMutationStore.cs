using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FakeDocumentMutationStore : ICottonDocumentMutationStore<string>
    {
        private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
        private int _deleteCallCount;
        private int _renameCallCount;

        public ISet<int> FailingDeleteCalls { get; } = new HashSet<int>();

        public ISet<int> FailingRenameCalls { get; } = new HashSet<int>();

        public List<string> Events { get; } = [];

        public void Add(string document, string displayName)
        {
            _names.Add(document, displayName);
        }

        public string GetName(string document)
        {
            return _names[document];
        }

        public bool Contains(string document)
        {
            return _names.ContainsKey(document);
        }

        public string Rename(string document, string displayName)
        {
            _renameCallCount++;
            Events.Add($"rename:{document}:{displayName}");
            if (FailingRenameCalls.Contains(_renameCallCount))
            {
                throw new IOException($"Rename call {_renameCallCount} failed.");
            }

            _names[document] = displayName;
            return document;
        }

        public void Delete(string document)
        {
            _deleteCallCount++;
            Events.Add($"delete:{document}");
            if (FailingDeleteCalls.Contains(_deleteCallCount))
            {
                throw new IOException($"Delete call {_deleteCallCount} failed.");
            }

            _names.Remove(document);
        }
    }
}
