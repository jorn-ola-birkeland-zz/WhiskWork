using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public class WorkItemQuery
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkItemRepository _workItemRepository;

        public WorkItemQuery(IWorkflowRepository repository, IWorkItemRepository workItems)
        {
            _workflowRepository = repository;
            _workItemRepository = workItems;
        }

        public bool IsDone(WorkItem item)
        {
            return _workflowRepository.GetWorkStep(item.Path).Type == WorkStepType.End;
        }

        public bool TryLocateWorkItem(string id, out WorkItem item)
        {
            item = _workItemRepository.GetWorkItem(id);

            return item != null;
        }

        public bool IsChildOfExpandedWorkItem(WorkItem item)
        {
            if (item.ParentId == null)
            {
                return false;
            }

            var parent = _workItemRepository.GetWorkItem(item.ParentId);
            var workStep = _workflowRepository.GetWorkStep(parent.Path);

            return workStep.Type == WorkStepType.Transient;
        }

        public bool IsExpandLocked(WorkItem item)
        {
            return item.Status == WorkItemStatus.ExpandLocked;
        }

        public bool IsChildOfParallelledWorkItem(WorkItem workItem)
        {
            if (workItem.ParentId != null)
            {
                WorkItem parent = _workItemRepository.GetWorkItem(workItem.ParentId);
                if (parent.Status == WorkItemStatus.ParallelLocked)
                {
                    return true;
                }
            }

            return false;
        }




        public bool IsParallelLockedWorkItem(WorkItem workItem)
        {
            return workItem.Status == WorkItemStatus.ParallelLocked;
        }

        public int GetNextOrdinal(WorkItem workItem)
        {
            var workItemsInStep = _workItemRepository.GetWorkItems(workItem.Path);

            return workItemsInStep.Count()>0 ? workItemsInStep.Max(wi => wi.Ordinal) + 1 : 1;
        }
    }
}