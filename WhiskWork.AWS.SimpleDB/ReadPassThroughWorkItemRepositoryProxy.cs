using System;
using System.Collections.Generic;
using WhiskWork.Core;

namespace WhiskWork.AWS.SimpleDB
{
    public abstract class ReadPassThroughWorkItemRepositoryProxy: ICacheableWorkItemRepository
    {

        protected ReadPassThroughWorkItemRepositoryProxy(ICacheableWorkItemRepository innerRepository)
        {
            InnerRepository = innerRepository;
        }

        protected ICacheableWorkItemRepository InnerRepository { get; private set; }

        public bool ExistsWorkItem(string id)
        {
            return InnerRepository.ExistsWorkItem(id);
        }

        public WorkItem GetWorkItem(string id)
        {
            return InnerRepository.GetWorkItem(id);
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return InnerRepository.GetWorkItems(path);
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            return InnerRepository.GetChildWorkItems(parent);
        }

        public IEnumerable<WorkItem> GetAllWorkItems()
        {
            return InnerRepository.GetAllWorkItems();
        }

        public abstract void CreateWorkItem(WorkItem workItem);


        public abstract void UpdateWorkItem(WorkItem workItem);


        public abstract void DeleteWorkItem(string workItemId);
    }
}