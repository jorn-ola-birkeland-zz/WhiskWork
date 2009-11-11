namespace WhiskWork.Core
{
    internal class WorkItemTransition
    {
        public WorkItemTransition(WorkItem workItem, WorkStep workStep)
        {
            WorkItem = workItem;
            WorkStep = workStep;
        }

        public WorkItem WorkItem { get; private set; }
        public WorkStep WorkStep{ get; private set; }
    }
}