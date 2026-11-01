// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json.Serialization;
using Cotton.Auth;

namespace Cotton.Mobile.Services
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(TokenPairDto))]
    internal partial class CottonTokenPairJsonContext : JsonSerializerContext
    {
    }
}
