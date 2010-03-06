using System;
using System.Linq;

namespace WhiskWork.Core
{
    internal class WorkItemRemover : WorkflowRepositoryInteraction
    {
        public WorkItemRemover(IWorkflowRepository workflow) : base(workflow)
        {
        }

        public void CleanUpIfInExpandStep(WorkItem workItemToMove)
        {
            WorkStep expandStep;
            if (WorkflowRepository.IsInExpandStep(workItemToMove, out expandStep))
            {
                var transientStepPath = ExpandedWorkStep.GetTransientPath(expandStep, workItemToMove);
                var transientStep = WorkflowRepository.GetWorkStep(transientStepPath);

                WorkflowRepository.DeleteWorkStepsRecursively(transientStep);
            }
        }

        public void DeleteWorkItem(string id)
        {
            using(WorkflowRepository.BeginTransaction())
            {
                var workItem = WorkflowRepository.GetWorkItem(id);

                ThrowInvalidOperationExceptionIfParentIsParallelLocked(workItem);

                DeleteWorkItemRecursively(workItem);

                WorkflowRepository.CommitTransaction();
            }
        }

        private void ThrowInvalidOperationExceptionIfParentIsParallelLocked(WorkItem workItem)
        {
            if (workItem.Parent != null)
            {
                var parent = WorkflowRepository.GetWorkItem(workItem.Parent.Id);
                if (parent.Status == WorkItemStatus.ParallelLocked)
                {
                    throw new InvalidOperationException("Cannot delete workitem which is child of paralleled workitem");
                }
            }
        }

        private void DeleteWorkItemRecursively(WorkItem workItem)
        {
            var expandedChildWorkItems = WorkflowRepository.GetChildWorkItems(workItem.AsParent(WorkItemParentType.Expanded));
            var parallelChildWorkItems = WorkflowRepository.GetChildWorkItems(workItem.AsParent(WorkItemParentType.Parallelled));

            var childWorkItems = expandedChildWorkItems.Concat(parallelChildWorkItems);

            if (childWorkItems.Count() > 0)
            {
                foreach (var childWorkItem in childWorkItems)
                {
                    DeleteWorkItemRecursively(childWorkItem);
                }
            }


            WorkflowRepository.DeleteWorkItem(workItem.Id);
            CleanUpIfInExpandStep(workItem);
        }



    }
}