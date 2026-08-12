// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.ViewModels
{
    public class MainPageProfile(
        string name,
        string? email,
        string instance,
        string accountScopeKey,
        Uri? avatarUrl)
    {
        public string Name { get; } = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name.Trim();

        public string? Email { get; } = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        public string Instance { get; } = string.IsNullOrWhiteSpace(instance) ? throw new ArgumentException("Profile instance is required.", nameof(instance)) : instance.Trim();

        public string AccountScopeKey { get; } = string.IsNullOrWhiteSpace(accountScopeKey)
                ? throw new ArgumentException("Account scope key is required.", nameof(accountScopeKey))
                : accountScopeKey.Trim();

        public Uri? AvatarUrl { get; } = avatarUrl;
    }
}
