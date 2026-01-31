using UnityEngine;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    public class JobManager : MonoBehaviour
    {
        public static JobManager Instance { get; private set; }

        public int TotalJobCount => 0;
        public int PendingJobCount => 0;
        public int ReadyJobCount => 0;
        public int ActiveJobCount => 0;

        private void Awake() { }
        private void Start() { }
        private void Update() { }

        public JobInstance CreateJob(JobDefinition definition, uint rescheduleCount = 0) => null;
        public void DestroyJob(JobInstance job) { }
        public List<JobInstance> GetReadyJobs() => null;
        public JobInstance GetNearestReadyJob(object location) => null;
        public bool ClaimJob(JobInstance job) => false;
        public bool StartJobExecution(JobInstance job) => false;
    }
}
