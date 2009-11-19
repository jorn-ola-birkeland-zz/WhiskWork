using System;
using System.Linq;

namespace WhiskWork.Core
{
    internal class WorkItemRemover : WorkflowRepositoryInteraction
    {
        public WorkItemRemover(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository) : base(workStepRepository, workItemRepository)
        {
        }

        public void CleanUpIfInExpandStep(WorkItem workItemToMove)
        {
            WorkStep expandStep;
            if (WorkStepRepository.IsInExpandStep(workItemToMove, out expandStep))
            {
                var transientStepPath = ExpandedWorkStep.GetTransientPath(expandStep, workItemToMove);
                var transientStep = WorkStepRepository.GetWorkStep(transientStepPath);
                
                //DeleteChildWorkItems(workItemToMove.ToParent(WorkItemParentType.Expanded));
                WorkStepRepository.DeleteWorkStepsRecursively(transientStep);
            }
        }

        private void DeleteChildWorkItems(WorkItemParent parent)
        {
            foreach (var childWorkItem in WorkItemRepository.GetChildWorkItems(parent))
            {
                DeleteWorkItem(childWorkItem.Id);
            }
        }

        public void DeleteWorkItem(string id)
        {
            var workItem = WorkItemRepository.GetWorkItem(id);

            ThrowInvalidOperationExceptionIfParentIsParallelLocked(workItem);

            DeleteWorkItemRecursively(workItem);
        }

        private void ThrowInvalidOperationExceptionIfParentIsParallelLocked(WorkItem workItem)
        {
            if (workItem.Parent != null)
            {
                var parent = WorkItemRepository.GetWorkItem(workItem.Parent.Id);
                if (parent.Status == WorkItemStatus.ParallelLocked)
                {
                    throw new InvalidOperationException("Cannot delete workitem which is child of paralleled workitem");
                }
            }
        }

        private void DeleteWorkItemRecursively(WorkItem workItem)
        {
            var expandedChildWorkItems = WorkItemRepository.GetChildWorkItems(workItem.ToParent(WorkItemParentType.Expanded));
            var parallelChildWorkItems = WorkItemRepository.GetChildWorkItems(workItem.ToParent(WorkItemParentType.Parallelled));

            var childWorkItems = expandedChildWorkItems.Concat(parallelChildWorkItems);

            if (childWorkItems.Count() > 0)
            {
                foreach (var childWorkItem in childWorkItems)
                {
                    DeleteWorkItemRecursively(childWorkItem);
                }
            }


            WorkItemRepository.DeleteWorkItem(workItem);
            CleanUpIfInExpandStep(workItem);
        }



    }
}