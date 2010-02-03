using System.Collections.Generic;
using WhiskWork.Core;

namespace WhiskWork.Synchronizer
{
    public interface IWhiskWorkRepository
    {
        IEnumerable<WorkItem> GetWorkItems();
        void PostWorkItem(WorkItem workItemUpdate);
        void DeleteWorkItem(WorkItem workItem);
    }
}