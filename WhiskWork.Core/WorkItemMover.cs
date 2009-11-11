using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WhiskWork.Core
{
    public class WorkflowRepositoryInteraction
    {
        public WorkflowRepositoryInteraction(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository)
        {
            WorkStepRepository = workStepRepository;
            WorkItemRepository = workItemRepository;
        }

        protected IWorkStepRepository WorkStepRepository { get; private set; }

        protected IWorkItemRepository WorkItemRepository { get; private set; }
    }

    internal class WorkItemMover : WorkflowRepositoryInteraction
    {
        public WorkItemMover(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository) : base(workStepRepository, workItemRepository)
        {
        }

        public void MoveWorkItem(WorkItem workItem, WorkStep toStep)
        {
            var transition = new WorkItemTransition(workItem, toStep);

            ThrowIfMovingParallelLockedWorkItem(transition);

            ThrowIfMovingExpandLockedWorkItem(transition);

            ThrowIfMovingFromTransientStepToParallelStep(transition);

            transition = CreateTransitionIfMovingToWithinParallelStep(transition);

            ThrowIfMovingToWithinExpandStep(transition);

            ThrowIfMovingToStepWithWrongClass(transition);

            transition = CreateTransitionIfMovingToExpandStep(transition);

            transition = CleanUpIfMovingFromTransientStep(transition);

            transition = AttemptMergeIfMovingChildOfParallelledWorkItem(transition);

            var resultTransition = DoMove(transition);

            TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(resultTransition);

        }

        private void ThrowIfMovingParallelLockedWorkItem(WorkItemTransition transition)
        {
            if (WorkItemRepository.IsParallelLockedWorkItem(transition.WorkItem))
            {
                throw new InvalidOperationException("Work item is locked for parallel work");
            }
        }

        private void ThrowIfMovingExpandLockedWorkItem(WorkItemTransition transition)
        {
            if (WorkItemRepository.IsExpandLockedWorkItem(transition.WorkItem))
            {
                throw new InvalidOperationException("Item is expandlocked and cannot be moved");
            }
        }

        private void ThrowIfMovingFromTransientStepToParallelStep(WorkItemTransition transition)
        {
            WorkStep transientStep;

            var isInTransientStep = WorkStepRepository.IsInTransientStep(transition.WorkItem, out transientStep);

            WorkStep parallelStepRoot;
            var isWithinParallelStep = WorkStepRepository.IsWithinParallelStep(transition.WorkStep, out parallelStepRoot);

            if (isInTransientStep && isWithinParallelStep)
            {
                throw new InvalidOperationException("Cannot move directly from transient step to parallelstep");
            }
        }

        private WorkItemTransition CreateTransitionIfMovingToWithinParallelStep(WorkItemTransition transition)
        {
            WorkStep parallelStep;
            if (WorkStepRepository.IsWithinParallelStep(transition.WorkStep, out parallelStep))
            {
                if (!WorkItemRepository.IsChildOfParallelledWorkItem(transition.WorkItem))
                {
                    var idToMove = ParallelStepHelper.GetParallelId(transition.WorkItem.Id, parallelStep, transition.WorkStep);
                    var workItemToMove = MoveAndLockAndSplitForParallelism(transition.WorkItem, parallelStep).First(wi => wi.Id == idToMove);

                    return new WorkItemTransition(workItemToMove, transition.WorkStep);
                }
            }
            return transition;
        }

        private void ThrowIfMovingToWithinExpandStep(WorkItemTransition transition)
        {
            WorkStep dummyStep;
            if (!WorkStepRepository.IsExpandStep(transition.WorkStep) && !WorkStepRepository.IsWithinTransientStep(transition.WorkStep, out dummyStep) && WorkStepRepository.IsWithinExpandStep(transition.WorkStep))
            {
                throw new InvalidOperationException("Cannot move item to within expand step");
            }
        }

        private void ThrowIfMovingToStepWithWrongClass(WorkItemTransition transition)
        {
            if (!WorkStepRepository.IsValidWorkStepForWorkItem(transition.WorkItem, transition.WorkStep))
            {
                throw new InvalidOperationException("Invalid step for work item");
            }
        }

        private WorkItemTransition CreateTransitionIfMovingToExpandStep(WorkItemTransition transition)
        {
            if (WorkStepRepository.IsExpandStep(transition.WorkStep))
            {
                var stepToMoveTo = CreateTransientWorkSteps(transition.WorkItem, transition.WorkStep);
                var workItemToMove = transition.WorkItem.AddClass(stepToMoveTo.WorkItemClass);

                transition = new WorkItemTransition(workItemToMove, stepToMoveTo);
            }
            return transition;
        }

        private WorkItemTransition CleanUpIfMovingFromTransientStep(WorkItemTransition transition)
        {
            var remover = new WorkItemRemover(WorkStepRepository, WorkItemRepository);

            transition = new WorkItemTransition(remover.CleanUpIfInTransientStep(transition.WorkItem), transition.WorkStep);
            return transition;
        }

        private WorkItemTransition AttemptMergeIfMovingChildOfParallelledWorkItem(WorkItemTransition transition)
        {
            if (WorkItemRepository.IsMergeableParallelledChild(transition.WorkItem, transition.WorkStep))
            {
                var workItemToMove = MergeParallelWorkItems(transition.WorkItem);

                transition = new WorkItemTransition(workItemToMove, transition.WorkStep);
            }

            return transition;
        }

        private WorkItemTransition DoMove(WorkItemTransition transition)
        {
            var fromStepPath = transition.WorkItem.Path;

            var movedWorkItem = transition.WorkItem.MoveTo(transition.WorkStep);

            var ordinal = WorkItemRepository.GetNextOrdinal(movedWorkItem);
            movedWorkItem = movedWorkItem.UpdateOrdinal(ordinal);

            WorkItemRepository.UpdateWorkItem(movedWorkItem);
            WorkItemRepository.RenumOrdinals(fromStepPath);

            return new WorkItemTransition(movedWorkItem, transition.WorkStep);
        }

        private void TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(WorkItemTransition resultTransition)
        {
            if (IsChildOfExpandedWorkItem(resultTransition.WorkItem))
            {
                TryUpdatingExpandLockOnParent(resultTransition.WorkItem);
            }
        }

        private IEnumerable<WorkItem> MoveAndLockAndSplitForParallelism(WorkItem item, WorkStep parallelRootStep)
        {
            var lockedAndMovedItem = item.MoveTo(parallelRootStep).UpdateStatus(WorkItemStatus.ParallelLocked);
            WorkItemRepository.UpdateWorkItem(lockedAndMovedItem);

            var helper = new ParallelStepHelper(WorkStepRepository);

            var splitWorkItems = helper.SplitForParallelism(item, parallelRootStep);

            foreach (var splitWorkItem in splitWorkItems)
            {
                WorkItemRepository.CreateWorkItem(splitWorkItem);
            }

            return splitWorkItems;
        }

        private WorkStep CreateTransientWorkSteps(WorkItem item, WorkStep expandStep)
        {
            Debug.Assert(expandStep.Type == WorkStepType.Expand);

            var transientRootPath = WorkStep.CombinePath(expandStep.Path, item.Id);

            CreateTransientWorkStepsRecursively(transientRootPath, expandStep, item.Id);

            var workItemClass = WorkItemClass.Combine(expandStep.WorkItemClass, item.Id);
            var transientWorkStep = new WorkStep(transientRootPath, expandStep.Path, expandStep.Ordinal, WorkStepType.Transient, workItemClass, expandStep.Title);
            WorkStepRepository.CreateWorkStep(transientWorkStep);

            return transientWorkStep;
        }

        private WorkItem MergeParallelWorkItems(WorkItem item)
        {
            var unlockedParentWorkItem = WorkItemRepository.GetWorkItem(item.ParentId).UpdateStatus(WorkItemStatus.Normal);
            WorkItemRepository.UpdateWorkItem(unlockedParentWorkItem);

            foreach (var childWorkItem in WorkItemRepository.GetChildWorkItems(item.ParentId).ToList())
            {
                WorkItemRepository.DeleteWorkItem(childWorkItem);
            }

            return unlockedParentWorkItem;
        }

        private void TryUpdatingExpandLockOnParent(WorkItem item)
        {
            var parent = WorkItemRepository.GetWorkItem(item.ParentId);

            if (WorkItemRepository.GetChildWorkItems(parent.Id).All(IsDone))
            {
                parent = parent.UpdateStatus(WorkItemStatus.Normal);
            }
            else
            {
                parent = parent.UpdateStatus(WorkItemStatus.ExpandLocked);
            }

            WorkItemRepository.UpdateWorkItem(parent);
        }

        private void CreateTransientWorkStepsRecursively(string transientRootPath, WorkStep rootStep, string workItemId)
        {
            var subSteps = WorkStepRepository.GetChildWorkSteps(rootStep.Path).Where(ws => ws.Type != WorkStepType.Transient);
            foreach (var childStep in subSteps)
            {
                var offset = childStep.Path.Remove(0, rootStep.Path.Length);

                var childTransientPath = transientRootPath + offset;

                var workItemClass = WorkItemClass.Combine(childStep.WorkItemClass, workItemId);
                WorkStepRepository.CreateWorkStep(new WorkStep(childTransientPath, transientRootPath, childStep.Ordinal, childStep.Type, workItemClass, childStep.Title));

                CreateTransientWorkStepsRecursively(childTransientPath, childStep, workItemId);
            }
        }

        private bool IsChildOfExpandedWorkItem(WorkItem item)
        {
            if (item.ParentId == null)
            {
                return false;
            }

            var parent = WorkItemRepository.GetWorkItem(item.ParentId);
            var workStep = WorkStepRepository.GetWorkStep(parent.Path);

            return workStep.Type == WorkStepType.Transient;
        }



        private bool IsDone(WorkItem item)
        {
            return WorkStepRepository.GetWorkStep(item.Path).Type == WorkStepType.End;
        }



    }
}