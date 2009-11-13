namespace WhiskWork.Core
{
    public interface IWorkflowLogger
    {
        void LogDeleteWorkItem(string id);
        void LogCreateWorkStep(WorkStep workStep);
        void LogCreateWorkItem(WorkItem workItem);
        void LogUpdateWorkItem(WorkItem oldWorkItem, WorkItem newWorkItem);
    }
}