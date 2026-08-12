// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using EasyExtensions.Mediator;

namespace Cotton.Mobile.Services
{
    public class RunSyncRootRequestHandler(SyncExecutionWorkflow workflow) :
        IRequestHandler<RunSyncRootRequest, string>
    {
        private readonly SyncExecutionWorkflow _workflow =
            workflow ?? throw new ArgumentNullException(nameof(workflow));

        public Task<string> Handle(
            RunSyncRootRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return _workflow.RunRootAsync(
                request.InstanceUri,
                request.Root,
                request.ReportStatus,
                cancellationToken);
        }
    }
}
