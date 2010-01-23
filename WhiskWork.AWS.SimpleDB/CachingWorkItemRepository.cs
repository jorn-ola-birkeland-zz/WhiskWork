using System;
using System.Collections.Generic;
using System.Linq;
using WhiskWork.Core;
using System.Threading;

namespace WhiskWork.AWS.SimpleDB
{
    public class CachingWorkItemRepository : IWorkItemRepository
    {
        private readonly Dictionary<string, WorkItem> _workItems;
        private readonly ICacheableWorkItemRepository _innerRepository;

        public CachingWorkItemRepository(ICacheableWorkItemRepository innerRepository)
        {
            _innerRepository = innerRepository;
            _workItems = LoadWorkItems();
        }

        private Dictionary<string, WorkItem> LoadWorkItems()
        {
            var workItems = new Dictionary<string, WorkItem>();

            foreach (var workItem in _innerRepository.GetAllWorkItems())
            {
                workItems.Add(workItem.Id, workItem); 
            }

            return workItems;
        }

        public bool ExistsWorkItem(string id)
        {
            return _workItems.ContainsKey(id);
        }

        public WorkItem GetWorkItem(string id)
        {
            if (!_workItems.ContainsKey(id))
            {
                throw new ArgumentException("Work item not found");
            }

            return _workItems[id];
        }


        public void CreateWorkItem(WorkItem workItem)
        {
            if (_workItems.ContainsKey(workItem.Id))
            {
                throw new ArgumentException("Work item already exists: " + workItem.Id);
            }

            _innerRepository.CreateWorkItem(workItem);
            _workItems.Add(workItem.Id, workItem);
        }


        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return _workItems.Values.Where(wi => wi.Path == path).ToList();
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            _innerRepository.UpdateWorkItem(workItem);

            _workItems[workItem.Id] = workItem;
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            return _workItems.Values.Where(wi => wi.Parent != null && wi.Parent.Id == parent.Id && wi.Parent.Type == parent.Type).ToList();
        }

        public void DeleteWorkItem(string workItemId)
        {
            _innerRepository.DeleteWorkItem(workItemId);

            _workItems.Remove(workItemId);
        }

    }
}