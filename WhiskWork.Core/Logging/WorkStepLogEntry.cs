namespace WhiskWork.Core.Logging
{
    public class WorkStepLogEntry
    {
        public WorkStep WorkStep { get; set; }

        public static WorkStepLogEntry CreateEntry(WorkStep workStep)
        {
            return new WorkStepLogEntry {WorkStep = workStep};
        }
    }
}