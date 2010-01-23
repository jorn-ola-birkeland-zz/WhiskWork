using System.Collections.Generic;

namespace WhiskWork.Core
{
    public interface IReadableWorkItemRepository
    {
        bool ExistsWorkItem(string id);
        WorkItem GetWorkItem(string id);
        IEnumerable<WorkItem> GetWorkItems(string path);
        IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent);
    }

    public interface IWriteableWorkItemRepository
    {
        void CreateWorkItem(WorkItem workItem);
        void UpdateWorkItem(WorkItem workItem);
        void DeleteWorkItem(string workItemId);
    }

    public interface IWorkItemRepository : IReadableWorkItemRepository, IWriteableWorkItemRepository
    {
    }

    public interface  ICacheableWorkItemRepository : IWorkItemRepository
    {
        IEnumerable<WorkItem> GetAllWorkItems();
    }
}