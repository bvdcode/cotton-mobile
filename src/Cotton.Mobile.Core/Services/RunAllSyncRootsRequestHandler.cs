// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using EasyExtensions.Mediator;

namespace Cotton.Mobile.Services
{
    public class RunAllSyncRootsRequestHandler(SyncExecutionWorkflow workflow) :
        IRequestHandler<RunAllSyncRootsRequest, string>
    {
        private readonly SyncExecutionWorkflow _workflow =
            workflow ?? throw new ArgumentNullException(nameof(workflow));

        public Task<string> Handle(
            RunAllSyncRootsRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return _workflow.RunAllAsync(
                request.InstanceUri,
                request.Roots,
                cancellationToken);
        }
    }
}
