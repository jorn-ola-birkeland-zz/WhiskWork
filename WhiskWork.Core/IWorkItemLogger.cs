namespace WhiskWork.Core
{
    public interface IWorkItemLogger
    {
        void LogCreate(WorkItem workItem);
        void LogUpdate(WorkItem oldWorkItem, WorkItem newWorkItem);
        void LogDelete(WorkItem workItem);
    }
}