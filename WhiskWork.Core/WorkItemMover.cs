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

            //ThrowIfMovingFromExpandStepToParallelStep(transition);
            
            if (WorkStepRepository.IsWithinParallelStep(transition.WorkStep))
            {
                MoveToWithinParallelStep(transition);
            }
            else
            {
                ThrowIfMovingToWithinExpandStep(transition);

                ThrowIfMovingToStepWithWrongClass(transition);

                CleanUpIfMovingFromExpandStep(transition);

                transition = AttemptMergeIfMovingChildOfParallelledWorkItem(transition);

                transition = CreateTransitionIfMovingToExpandStep(transition);

                var resultTransition = DoMove(transition);

                TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(resultTransition);
            }
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

        private void ThrowIfMovingFromExpandStepToParallelStep(WorkItemTransition transition)
        {
            WorkStep expandStep;

            var isInExpandStep = WorkStepRepository.IsInExpandStep(transition.WorkItem, out expandStep);

            WorkStep parallelStepRoot;
            var isWithinParallelStep = WorkStepRepository.IsWithinParallelStep(transition.WorkStep, out parallelStepRoot);

            if (isInExpandStep && isWithinParallelStep)
            {
                throw new InvalidOperationException("Cannot move directly from expand step to parallelstep");
            }
        }

        private void MoveToWithinParallelStep(WorkItemTransition transition)
        {
            WorkStep parallelStep;
            if (!WorkStepRepository.IsWithinParallelStep(transition.WorkStep, out parallelStep))
            {
                return;
            }

            var newTransition = transition;

            if (!WorkItemRepository.IsChildOfParallelledWorkItem(transition.WorkItem))
            {
                WorkStep expandStep;
                var isInExpandStep = WorkStepRepository.IsInExpandStep(transition.WorkItem, out expandStep);

                var lockedAndMovedItem = transition.WorkItem.MoveTo(parallelStep).UpdateStatus(WorkItemStatus.ParallelLocked);
                WorkItemRepository.UpdateWorkItem(lockedAndMovedItem);

                if(isInExpandStep)
                {
                    CleanUpIfMovingFromExpandStep(transition);
                }

                var helper = new ParallelStepHelper(WorkStepRepository);

                var splitWorkItems = helper.SplitForParallelism(transition.WorkItem, parallelStep);

                foreach (var splitWorkItem in splitWorkItems)
                {
                    WorkItemRepository.CreateWorkItem(splitWorkItem);
                    
                    if(isInExpandStep)
                    {
                        CreateTransientWorkSteps(splitWorkItem, expandStep);
                    }
                }

                var idToMove = ParallelStepHelper.GetParallelId(transition.WorkItem.Id, parallelStep, transition.WorkStep);
                var workItemToMove = splitWorkItems.First(wi => wi.Id == idToMove);

                newTransition = new WorkItemTransition(workItemToMove, transition.WorkStep);
            }

            ThrowIfMovingToStepWithWrongClass(newTransition);

            CleanUpIfMovingFromExpandStep(newTransition);

            newTransition = AttemptMergeIfMovingChildOfParallelledWorkItem(newTransition);

            var resultTransition = DoMove(newTransition);

            TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(resultTransition);
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
                CreateTransientWorkSteps(transition.WorkItem, transition.WorkStep);
            }
            return transition;
        }

        private void CleanUpIfMovingFromExpandStep(WorkItemTransition transition)
        {
            var remover = new WorkItemRemover(WorkStepRepository, WorkItemRepository);
            remover.CleanUpIfInExpandStep(transition.WorkItem);
        }

        private WorkItemTransition AttemptMergeIfMovingChildOfParallelledWorkItem(WorkItemTransition transition)
        {
            if (WorkItemRepository.IsMergeableParallelledChild(transition.WorkItem, transition.WorkStep))
            {
                var workItemToMove = MergeParallelWorkItems(transition);

                transition = new WorkItemTransition(workItemToMove, transition.WorkStep);
            }

            return transition;
        }

        private WorkItemTransition DoMove(WorkItemTransition transition)
        {
            var movedWorkItem = transition.WorkItem.MoveTo(transition.WorkStep);

            WorkItemRepository.UpdateWorkItem(movedWorkItem);

            return new WorkItemTransition(movedWorkItem, transition.WorkStep);
        }

        private void TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(WorkItemTransition resultTransition)
        {
            if (IsChildOfExpandedWorkItem(resultTransition.WorkItem))
            {
                TryUpdatingExpandLockOnParent(resultTransition.WorkItem);
            }
        }

        private void CreateTransientWorkSteps(WorkItem item, WorkStep expandStep)
        {
            Debug.Assert(expandStep.Type == WorkStepType.Expand);

            var transientRootPath = ExpandedWorkStep.GetTransientPath(expandStep, item);

            CreateTransientWorkStepsRecursively(transientRootPath, expandStep, item.Id);

            var workItemClass = WorkItemClass.Combine(expandStep.WorkItemClass, item.Id);
            var transientWorkStep = new WorkStep(transientRootPath, expandStep.Path, expandStep.Ordinal, WorkStepType.Transient, workItemClass, expandStep.Title);
            WorkStepRepository.CreateWorkStep(transientWorkStep);
        }

        private WorkItem MergeParallelWorkItems(WorkItemTransition transition)
        {
            var unlockedParentWorkItem = WorkItemRepository.GetWorkItem(transition.WorkItem.Parent.Id).UpdateStatus(WorkItemStatus.Normal);
            WorkItemRepository.UpdateWorkItem(unlockedParentWorkItem);

            foreach (var childWorkItem in WorkItemRepository.GetChildWorkItems(transition.WorkItem.Parent).ToList())
            {
                if (WorkStepRepository.IsExpandStep(transition.WorkStep))
                {
                    CleanUpIfMovingFromExpandStep(new WorkItemTransition(childWorkItem, transition.WorkStep));
                }

                WorkItemRepository.DeleteWorkItem(childWorkItem);
            }

            return unlockedParentWorkItem;
        }

        private void TryUpdatingExpandLockOnParent(WorkItem item)
        {
            var parent = WorkItemRepository.GetWorkItem(item.Parent.Id);

            if (WorkItemRepository.GetChildWorkItems(item.Parent).All(IsDone))
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
            if (item.Parent == null)
            {
                return false;
            }

            var parent = WorkItemRepository.GetWorkItem(item.Parent.Id);
            var workStep = WorkStepRepository.GetWorkStep(parent.Path);

            return workStep.Type == WorkStepType.Expand;
        }



        private bool IsDone(WorkItem item)
        {
            return WorkStepRepository.GetWorkStep(item.Path).Type == WorkStepType.End;
        }



    }
}