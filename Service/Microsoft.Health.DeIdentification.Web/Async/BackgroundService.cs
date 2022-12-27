// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using EnsureThat;
using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web.Async
{
    public class BackgroundService
    {
        private readonly JobHosting _jobHosting;

        public BackgroundService(JobHosting jobHosting)
        {
            _jobHosting = EnsureArg.IsNotNull(jobHosting, nameof(jobHosting));
        }

        async Task<string> StartAsync(CancellationToken cancellationToken = default)
        {
            // start job hosting
            // We can leverage job hosting here for long running job
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _jobHosting.MaxRunningJobCount = 1;
            await _jobHosting.StartAsync((byte)QueueType.Deid, Environment.MachineName, cancellationTokenSource);
            return string.Empty;
        }
    }
}
