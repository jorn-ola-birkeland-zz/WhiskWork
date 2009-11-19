using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public class MemoryWorkItemRepository : IWorkItemRepository
    {
        private readonly Dictionary<string, WorkItem> _workItems = new Dictionary<string, WorkItem>();

        public WorkItem GetWorkItem(string id)
        {
            if(!_workItems.ContainsKey(id))
            {
                throw new ArgumentException("Work item not found");
            }

            return _workItems[id];
        }

        public void CreateWorkItem(WorkItem workItem)
        {
            _workItems.Add(workItem.Id, workItem); 
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return _workItems.Values.Where(wi => wi.Path == path).ToList();
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            _workItems[workItem.Id] = workItem;
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            return _workItems.Values.Where(wi => wi.Parent!=null && wi.Parent.Id == parent.Id && wi.Parent.Type == parent.Type).ToList();
        }

        public void DeleteWorkItem(WorkItem workItem)
        {
            _workItems.Remove(workItem.Id);
        }

        public bool ExistsWorkItem(string id)
        {
            return _workItems.ContainsKey(id);
        }
    }
}