#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace WhiskWork.Core
{
    public interface IWipLimitChecker
    {
        bool CanAcceptWorkItem(WorkItem workItem);
        bool CanAcceptWorkStep(WorkStep addToStep, WorkStep workStepToAdd);
    }

    public class WipLimitChecker : IWipLimitChecker
    {
        private readonly IWorkflowRepository _workflowRepository;

        public WipLimitChecker(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository)
        {
            _workflowRepository = new WorkflowRepository(workItemRepository,workStepRepository);
        }

        public WipLimitChecker(IWorkflowRepository workflowRepository)
        {
            _workflowRepository = workflowRepository;
        }

        public bool CanAcceptWorkItem(WorkItem workItem)
        {
            var workStep = _workflowRepository.GetWorkStep(workItem.Path);
            return CheckWipExclusiveWorkItems(workStep, new[] {workItem},1);
        }

        public bool CanAcceptWorkStep(WorkStep addToStep, WorkStep stepWithWorkItems)
        {
            var newWorkItems = _workflowRepository.GetWorkItemsRecursively(stepWithWorkItems);
            var currentWorkItems = _workflowRepository.GetWorkItemsRecursively(addToStep);

            var exclusivelyNewWorkItems = newWorkItems.Except(currentWorkItems);

            int count = exclusivelyNewWorkItems.Count();

            return CheckWipExclusiveWorkItems(addToStep, null, count);
        }

        public int CountWip(WorkStep workStep)
        {
            return CountSubTree(workStep, null, null);
        }

        private bool CheckWipExclusiveWorkItems(WorkStep workStep, IEnumerable<WorkItem> workItemsToExclude, int workItemsToAdd)
        {
            var currentWorkStep = workStep;
            WorkStep childStep = null;

            var wipCount = workItemsToAdd;
            var fullWipCount = 0;

            while (currentWorkStep != null)
            {
                wipCount += CountSubTree(currentWorkStep, workItemsToExclude, childStep);
                fullWipCount += CountSubTree(currentWorkStep, null, childStep);

                var allowed = !currentWorkStep.WipLimit.HasValue || currentWorkStep.WipLimit.Value >= wipCount || wipCount<=fullWipCount;
                if (!allowed)
                {
                    return false;
                }

                childStep = currentWorkStep;
                currentWorkStep = GetParentWorkStep(currentWorkStep);
            }

            return true;
        }

        private WorkStep GetParentWorkStep(WorkStep currentWorkStep)
        {
            var parentPath = currentWorkStep.ParentPath;

            if(parentPath == WorkStep.Root.Path)
            {
                return null;
            }

            currentWorkStep = _workflowRepository.GetWorkStep(parentPath);

            if(currentWorkStep.Type == WorkStepType.Parallel || currentWorkStep.Type == WorkStepType.Transient)
            {
                return null;
            }

            return currentWorkStep;
        }

        private int CountSubTree(WorkStep workStep, IEnumerable<WorkItem> workItemsToExclude, WorkStep childWorkStepToExclude)
        {
            var wipCount = 0;
            foreach (var item in _workflowRepository.GetWorkItems(workStep.Path))
            {
                if (workItemsToExclude==null || !workItemsToExclude.Select(wi=>wi.Id).Contains(item.Id))
                {
                    wipCount++;
                }
            }

            if(workStep.Type == WorkStepType.Parallel || workStep.Type == WorkStepType.Transient)
            {
                return wipCount;
            }   

            foreach (var childWorkStep in _workflowRepository.GetChildWorkSteps(workStep.Path))
            {
                if (childWorkStepToExclude != null && childWorkStepToExclude.Path == childWorkStep.Path)
                {
                    continue;
                }

                wipCount += CountSubTree(childWorkStep, workItemsToExclude, childWorkStepToExclude);
            }

            return wipCount;
        }
    }

}