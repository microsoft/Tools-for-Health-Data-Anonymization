// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.DeIdentification.Batch;
using Microsoft.Health.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Web.Async
{
    public class HostingBackgroundService : BackgroundService
    {
        private readonly JobHosting _jobHosting;
        private readonly IQueueClient _client;

        public HostingBackgroundService(JobHosting jobHosting,
            IQueueClient client)
        {
            _jobHosting = EnsureArg.IsNotNull(jobHosting, nameof(jobHosting));
            _client = EnsureArg.IsNotNull(client, nameof(client));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // start job hosting
            // We can leverage job hosting here for long running job
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _jobHosting.MaxRunningJobCount = 1;
            await _jobHosting.StartAsync((byte)QueueType.Deid, Environment.MachineName, cancellationTokenSource);
            
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
