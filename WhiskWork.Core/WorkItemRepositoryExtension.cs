using System.Linq;

namespace WhiskWork.Core
{
    public static class WorkItemRepositoryExtension
    {
        public static void RenumOrdinals(this IWorkItemRepository workItemRepository, string path)
        {
            var ordinal = 1;
            foreach (var workItem in workItemRepository.GetWorkItems(path).OrderBy(wi => wi.Ordinal))
            {
                workItemRepository.UpdateWorkItem(workItem.UpdateOrdinal(ordinal++));
            }

        }

        public static bool TryLocateWorkItem(this IWorkItemRepository workItemRepository, string id, out WorkItem item)
        {
            item = workItemRepository.GetWorkItem(id);

            return item != null;
        }


        public static bool IsExpandLockedWorkItem(this IWorkItemRepository workItemRepository, WorkItem item)
        {
            return item.Status == WorkItemStatus.ExpandLocked;
        }

        public static bool IsChildOfParallelledWorkItem(this IWorkItemRepository workItemRepository, WorkItem workItem)
        {
            if (workItem.ParentId != null)
            {
                var parent = workItemRepository.GetWorkItem(workItem.ParentId);
                if (parent.Status == WorkItemStatus.ParallelLocked)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsMergeableParallelledChild(this IWorkItemRepository workItemRepository, WorkItem item, WorkStep toStep)
        {
            if (!workItemRepository.IsChildOfParallelledWorkItem(item))
            {
                return false;
            }

            var isMergeable = true;
            foreach (var childWorkItem in workItemRepository.GetChildWorkItems(item.ParentId).Where(wi => wi.Id != item.Id))
            {
                isMergeable &= childWorkItem.Path == toStep.Path;
            }
            return isMergeable;
        }



        public static bool IsParallelLockedWorkItem(this IWorkItemRepository workItemRepository, WorkItem workItem)
        {
            return workItem.Status == WorkItemStatus.ParallelLocked;
        }

        public static int GetNextOrdinal(this IWorkItemRepository workItemRepository, WorkItem workItem)
        {
            var workItemsInStep = workItemRepository.GetWorkItems(workItem.Path);

            return workItemsInStep.Count() > 0 ? workItemsInStep.Max(wi => wi.Ordinal) + 1 : 1;
        }


    }
}