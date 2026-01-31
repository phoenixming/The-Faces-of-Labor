namespace FacesOfLabor.Core
{
    public class JobInstance
    {
        public readonly uint InstanceId;
        public JobDefinition Definition;
        public JobState State;
        public uint Reschedule;
        public float TimeRemaining;

        public JobType Type => Definition.Type;
        public float Progress => 0f;

        public JobInstance(JobDefinition definition, uint rescheduleCount = 0) { }
        public bool SetState(JobState newState) => false;
        public void UpdateExecution(float deltaTime) { }
        public JobInstance CreateRescheduledInstance() => null;
    }
}
