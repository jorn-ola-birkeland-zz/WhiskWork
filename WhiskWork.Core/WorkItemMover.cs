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

            if (IsMovingWithinParallelStep(transition))
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

        private bool IsMovingWithinParallelStep(WorkItemTransition transition)
        {
            return WorkStepRepository.IsWithinParallelStep(transition.WorkStep);
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

        private void MoveToWithinParallelStep(WorkItemTransition transition)
        {
            var newTransition = transition;

            if(!WorkStepRepository.IsValidWorkStepForWorkItem(transition.WorkItem,transition.WorkStep))
            {
                newTransition = TraverseParallelMoveHierarchy(transition);
            }

            ThrowIfMovingToStepWithWrongClass(newTransition);

            CleanUpIfMovingFromExpandStep(newTransition);

            newTransition = AttemptMergeIfMovingChildOfParallelledWorkItem(newTransition);

            var resultTransition = DoMove(newTransition);

            TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(resultTransition);
        }

        private bool HasParallelParentInPath(WorkItemTransition transition)
        {
            var parent = transition.WorkItem.Parent;

            while (parent != null)
            {
                var parentWorkItem = WorkItemRepository.GetWorkItem(parent.Id);
                if (parent.Type == WorkItemParentType.Parallelled)
                {
                    if (transition.WorkStep.Path.StartsWith(parentWorkItem.Path))
                    {
                        return true;
                    }
                }

                parent = parentWorkItem.Parent;
            }

            return false;
        }

        private WorkItemTransition TraverseParallelMoveHierarchy(WorkItemTransition transition)
        {
            var pathsBetweenRootAndTarget = GetPathsToTraverseForParallelStep(transition);

            return TraversePathsForParallelStep(transition, pathsBetweenRootAndTarget);
        }

        private IEnumerable<string> GetPathsToTraverseForParallelStep(WorkItemTransition transition)
        {
            IEnumerable<string> pathsBetweenRootAndTarget;
            WorkItem parallelParent;
            if (IsMovedUnderneathParallelParent(transition, out parallelParent))
            {
                var commonRootStepPath = WorkflowPath.FindCommonRoot(parallelParent.Path, transition.WorkStep.Path);
                pathsBetweenRootAndTarget = WorkflowPath.GetPathsBetween(commonRootStepPath, transition.WorkStep.Path).Skip(1);
            }
            else
            {
                var commonRootStepPath = WorkflowPath.FindCommonRoot(transition.WorkItem.Path, transition.WorkStep.Path);
                pathsBetweenRootAndTarget = WorkflowPath.GetPathsBetween(commonRootStepPath, transition.WorkStep.Path);
            }

            return pathsBetweenRootAndTarget;
        }

        private bool IsMovedUnderneathParallelParent(WorkItemTransition transition, out WorkItem parallelParent)
        {
            if(WorkItemRepository.IsChildOfParallelledWorkItem(transition.WorkItem, out parallelParent))
            {
                return transition.WorkStep.Path.StartsWith(parallelParent.Path);
            }

            return false;
        }


        private WorkItemTransition TraversePathsForParallelStep(WorkItemTransition transition, IEnumerable<string> pathsBetweenRootAndTarget)
        {
            var currentWorkItem = transition.WorkItem;

            WorkStep parentWorkStep = null;

            foreach (var inBetweenPath in pathsBetweenRootAndTarget)
            {
                var currentWorkStep = WorkStepRepository.GetWorkStep(inBetweenPath);
                var currentTransition = new WorkItemTransition(currentWorkItem,currentWorkStep);

                if(parentWorkStep!=null &&  WorkStepRepository.IsParallelStep(parentWorkStep))
                {
                    var transitionToParallelRoot = new WorkItemTransition(currentWorkItem, parentWorkStep);
                    LockAndMoveToParallelRoot(transitionToParallelRoot);

                    currentWorkItem = CreateParallelledChildrenAndReturnWorkItemToMove(currentTransition, parentWorkStep);
                }

                parentWorkStep = currentWorkStep;
            }

            return new WorkItemTransition(currentWorkItem, transition.WorkStep);
        }

        private void LockAndMoveToParallelRoot(WorkItemTransition transition)
        {
            ThrowIfMovingToStepWithWrongClass(transition);

            var lockedAndMovedItem = transition.WorkItem.MoveTo(transition.WorkStep).UpdateStatus(WorkItemStatus.ParallelLocked);
            WorkItemRepository.UpdateWorkItem(lockedAndMovedItem);

            if (WorkStepRepository.IsInExpandStep(transition.WorkItem))
            {
                CleanUpIfMovingFromExpandStep(transition);
            }
        }

        private WorkItem CreateParallelledChildrenAndReturnWorkItemToMove(WorkItemTransition transition, WorkStep parallelStep)
        {
            var helper = new ParallelStepHelper(WorkStepRepository);

            var splitWorkItems = helper.SplitForParallelism(transition.WorkItem, parallelStep);

            foreach (var splitWorkItem in splitWorkItems)
            {
                WorkItemRepository.CreateWorkItem(splitWorkItem);

                WorkStep expandStep;
                var isInExpandStep = WorkStepRepository.IsInExpandStep(transition.WorkItem, out expandStep);
                if (isInExpandStep)
                {
                    CreateTransientWorkSteps(splitWorkItem, expandStep);
                }
            }

            var idToMove = ParallelStepHelper.GetParallelId(transition.WorkItem.Id, parallelStep, transition.WorkStep);
            var workItemToMove = splitWorkItems.First(wi => wi.Id == idToMove);

            return workItemToMove;
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