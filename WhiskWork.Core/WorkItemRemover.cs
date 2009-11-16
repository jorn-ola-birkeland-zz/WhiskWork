using System;
using System.Linq;

namespace WhiskWork.Core
{
    internal class WorkItemRemover : WorkflowRepositoryInteraction
    {
        public WorkItemRemover(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository) : base(workStepRepository, workItemRepository)
        {
        }

        public WorkItem CleanUpIfInTransientStep(WorkItem workItemToMove)
        {
            WorkStep expandStep;
            if (WorkStepRepository.IsInExpandStep(workItemToMove, out expandStep))
            {
                var transientStepPath = ExpandedWorkStep.GetTransientPath(expandStep, workItemToMove);
                var transientStep = WorkStepRepository.GetWorkStep(transientStepPath);
                
                DeleteChildWorkItems(workItemToMove);
                WorkStepRepository.DeleteWorkStepsRecursively(transientStep);
                //workItemToMove = workItemToMove.RemoveClass(transientStep.WorkItemClass);
            }
            return workItemToMove;
        }

        private void DeleteChildWorkItems(WorkItem workItem)
        {
            foreach (var childWorkItem in WorkItemRepository.GetChildWorkItems(workItem.Id))
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
            if (workItem.ParentId != null)
            {
                var parent = WorkItemRepository.GetWorkItem(workItem.ParentId);
                if (parent.Status == WorkItemStatus.ParallelLocked)
                {
                    throw new InvalidOperationException("Cannot delete workitem which is child of paralleled workitem");
                }
            }
        }

        private void DeleteWorkItemRecursively(WorkItem workItem)
        {
            var childWorkItems = WorkItemRepository.GetChildWorkItems(workItem.Id);

            if (childWorkItems.Count() > 0)
            {
                foreach (var childWorkItem in childWorkItems)
                {
                    DeleteWorkItemRecursively(childWorkItem);
                }
            }


            WorkItemRepository.DeleteWorkItem(workItem);
            WorkItemRepository.RenumOrdinals(workItem.Path);
            CleanUpIfInTransientStep(workItem);
        }



    }
}