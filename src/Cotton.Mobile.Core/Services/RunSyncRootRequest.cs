// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using EasyExtensions.Mediator.Contracts;

namespace Cotton.Mobile.Services
{
    public record RunSyncRootRequest(
        Uri InstanceUri,
        CottonSyncRootSnapshot Root) : IRequest<string>;
}
