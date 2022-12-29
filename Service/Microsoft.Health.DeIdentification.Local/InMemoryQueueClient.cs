// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.JobManagement;

namespace Microsoft.Health.DeIdentification.Local
{
    public class InMemoryQueueClient : IQueueClient
    {
        private int defaultGroupId = 0;
        private List<JobInfo> jobInfos = new List<JobInfo>();
        public List<JobInfo> JobInfos
        {
            get { return jobInfos; } 
        }

        public Task CancelJobByGroupIdAsync(byte queueType, long groupId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task CancelJobByIdAsync(byte queueType, long jobId, CancellationToken cancellationToken)
        {
            foreach (JobInfo jobInfo in jobInfos.Where(t => t.Id == jobId))
            {
                if (jobInfo.Status == JobStatus.Created)
                {
                    jobInfo.Status = JobStatus.Cancelled;
                }

                jobInfo.CancelRequested = true;
            }

            return Task.CompletedTask;
        }

        public async Task CompleteJobAsync(JobInfo jobInfo, bool requestCancellationOnFailure, CancellationToken cancellationToken)
        {
            // TODO verify job result overwrite
            JobInfo jobInfoStore = jobInfos.FirstOrDefault(t => t.Id == jobInfo.Id);

            if (jobInfo.Status == JobStatus.Failed)
            {
                jobInfoStore.Status = JobStatus.Failed;
            } else if (jobInfo.CancelRequested = true)
            {
                jobInfoStore.Status = JobStatus.Cancelled;
            } else
            {
                jobInfoStore.Status = JobStatus.Completed;
            }

            jobInfoStore.Result = jobInfo.Result;

            if (requestCancellationOnFailure && jobInfo.Status == JobStatus.Failed)
            {
                await CancelJobByIdAsync(jobInfo.QueueType, jobInfo.Id, cancellationToken);
            }
        }

        public Task<JobInfo> DequeueAsync(byte queueType, string worker, int heartbeatTimeoutSec, CancellationToken cancellationToken, long? jobId = null)
        {
            try
            {
                JobInfo job = jobInfos.FirstOrDefault(t => t.Status == JobStatus.Created || (t.Status == JobStatus.Running && (DateTime.Now - t.HeartbeatDateTime) > TimeSpan.FromSeconds(heartbeatTimeoutSec)));
                if (job != null)
                {
                    job.Status = JobStatus.Running;
                    job.HeartbeatDateTime = DateTime.Now;
                }

                return Task.FromResult(job);
            } catch(Exception ex)
            {
                throw;
            }
            
        }

        public async Task<IReadOnlyCollection<JobInfo>> DequeueJobsAsync(byte queueType, int numberOfJobsToDequeue, string worker, int heartbeatTimeoutSec, CancellationToken cancellationToken)
        {
            try
            {
                if (numberOfJobsToDequeue > jobInfos.Count(t => t.Status == JobStatus.Created || t.Status == JobStatus.Running))
                {
                    throw new InvalidDataException("number exceed length");
                }
                var jobs = new List<JobInfo>();
                while(numberOfJobsToDequeue > 0)
                {
                    JobInfo job = jobInfos.FirstOrDefault(t => t.Status == JobStatus.Created || (t.Status == JobStatus.Running && (DateTime.Now - t.HeartbeatDateTime) > TimeSpan.FromSeconds(heartbeatTimeoutSec)));
                    if (job != null)
                    {
                        job.Status = JobStatus.Running;
                        job.HeartbeatDateTime = DateTime.Now;
                    }
                    jobs.Add(job);
                    numberOfJobsToDequeue--;
                }
                return jobs;
            } catch(Exception ex)
            {
                throw ex;
            }
        }

        public Task<IReadOnlyList<JobInfo>> EnqueueAsync(byte queueType, string[] definitions, long? groupId, bool forceOneActiveJobGroup, bool isCompleted, CancellationToken cancellationToken)
        {
            // TODO add lock for job enqueue and dequeue.
            var result = new List<JobInfo>();

            foreach (string definition in definitions)
            {
                if (jobInfos.Any(t => t.Definition.Equals(definition)))
                {
                    result.Add(jobInfos.First(t => t.Definition.Equals(definition)));
                    continue;
                }

                result.Add(new JobInfo()
                {
                    QueueType = queueType,
                    Result = string.Empty,
                    CancelRequested = false,
                    Definition = definition,
                    Id = BitConverter.ToInt64(Guid.NewGuid().ToByteArray()),
                    GroupId = defaultGroupId,
                    Status = JobStatus.Created,
                    HeartbeatDateTime = DateTime.Now,
                });
            }

            jobInfos.AddRange(result);
            return Task.FromResult<IReadOnlyList<JobInfo>>(result);
        }

        public Task<IReadOnlyList<JobInfo>> GetJobByGroupIdAsync(byte queueType, long groupId, bool returnDefinition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<JobInfo> GetJobByIdAsync(byte queueType, long jobId, bool returnDefinition, CancellationToken cancellationToken)
        {
            JobInfo result = jobInfos.FirstOrDefault(t => t.Id == jobId);
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<JobInfo>> GetJobsByIdsAsync(byte queueType, long[] jobIds, bool returnDefinition, CancellationToken cancellationToken)
        {
            var result = jobIds.Select(t => GetJobByIdAsync(queueType, t, returnDefinition, cancellationToken));
            return Task.FromResult<IReadOnlyList<JobInfo>>((List<JobInfo>)result);
        }

        public bool IsInitialized()
        {
            return true;
        }

        public Task<bool> KeepAliveJobAsync(JobInfo jobInfo, CancellationToken cancellationToken)
        {
            JobInfo job = jobInfos.FirstOrDefault(t => t.Id == jobInfo.Id);
            if (job == null)
            {
                throw new JobNotExistException($"{jobInfo.Id} job not exist");
            }
            job.HeartbeatDateTime = DateTime.Now;
            job.Result = jobInfo.Result;

            return Task.FromResult(job.CancelRequested);
        }
    }
}