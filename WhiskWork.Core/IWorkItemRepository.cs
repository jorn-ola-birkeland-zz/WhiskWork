using System.Collections.Generic;

namespace WhiskWork.Core
{
    public interface IWorkItemRepository
    {
        bool ExistsWorkItem(string id);
        WorkItem GetWorkItem(string id);
        void CreateWorkItem(WorkItem workItem);
        IEnumerable<WorkItem> GetWorkItems(string path);
        void UpdateWorkItem(WorkItem workItem);
        IEnumerable<WorkItem> GetChildWorkItems(string id);
        void DeleteWorkItem(WorkItem workItem);
    }
}