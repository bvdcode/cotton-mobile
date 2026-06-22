// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAuthenticatedApiResponse<T>
    {
        public CottonAuthenticatedApiResponse(
            T value,
            IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
        {
            ArgumentNullException.ThrowIfNull(headers);

            Value = value;
            Headers = headers.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        public T Value { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

        public string? GetHeaderValue(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Header name is required.", nameof(name));
            }

            return Headers.TryGetValue(name.Trim(), out IReadOnlyList<string>? values)
                ? values.FirstOrDefault()
                : null;
        }
    }
}
