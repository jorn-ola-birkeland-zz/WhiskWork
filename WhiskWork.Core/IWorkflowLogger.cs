namespace WhiskWork.Core
{
    public interface IWorkItemLogger
    {
        void LogDeleteWorkItem(string id);
        void LogCreateWorkStep(WorkStep workStep);
        void LogCreateWorkItem(WorkItem workItem);
        void LogUpdateWorkItem(WorkItem oldWorkItem, WorkItem newWorkItem);
    }
}