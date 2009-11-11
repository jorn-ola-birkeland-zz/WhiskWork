using System.Collections.Specialized;

namespace WhiskWork.Core
{
    public interface IWorkflow
    {
        bool ExistsWorkItem(string workItemId);
        bool ExistsWorkStep(string path);
        void UpdateWorkItem(WorkItem workItem);
        void CreateWorkStep(WorkStep workStep);
        void CreateWorkItem(WorkItem workItem);
        WorkItem GetWorkItem(string workItemId);
        void DeleteWorkItem(string workItemId);
    }
}