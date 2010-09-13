using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WhiskWork.Core.Exception;

namespace WhiskWork.Core
{
    public class WorkflowRepositoryInteraction
    {
        public WorkflowRepositoryInteraction(IWorkflowRepository workflowRepository)
        {
            WorkflowRepository = workflowRepository;
        }

        protected IWorkflowRepository WorkflowRepository { get; private set; }

    }

    public class WorkItemMover : WorkflowRepositoryInteraction
    {
        private readonly ITimeSource _timeSource;

        public WorkItemMover(IWorkflowRepository workflowRepository) : this(workflowRepository,new DefaultTimeSource())
        {
        }

        public WorkItemMover(IWorkflowRepository workflowRepository, ITimeSource timeSource) : base(workflowRepository)
        {
            _timeSource = timeSource;
            WipLimitChecker = new WipLimitChecker(workflowRepository);
        }

        public IWipLimitChecker WipLimitChecker { get; set; }

        public void MoveWorkItem(WorkItem workItem, WorkStep toStep)
        {
            using(WorkflowRepository.BeginTransaction())
            {
                Move(workItem, toStep);
                WorkflowRepository.CommitTransaction();
            }
        }

        private void Move(WorkItem workItem, WorkStep toStep)
        {
            var transition = new WorkItemTransition(workItem, toStep);

            ThrowIfMovingParallelLockedWorkItem(transition);

            ThrowIfMovingExpandLockedWorkItem(transition);

            ThrowIfViolatingWipLimit(transition);

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
            return WorkflowRepository.IsWithinParallelStep(transition.WorkStep);
        }

        private void ThrowIfMovingParallelLockedWorkItem(WorkItemTransition transition)
        {
            if (WorkflowRepository.IsParallelLockedWorkItem(transition.WorkItem))
            {
                throw new InvalidOperationException("Work item is locked for parallel work");
            }
        }

        private void ThrowIfMovingExpandLockedWorkItem(WorkItemTransition transition)
        {
            if (WorkflowRepository.IsExpandLockedWorkItem(transition.WorkItem))
            {
                throw new InvalidOperationException("Item is expandlocked and cannot be moved");
            }
        }

        private void ThrowIfViolatingWipLimit(WorkItemTransition transition)
        {
            if(!WipLimitChecker.CanAcceptWorkItem(transition.WorkItem.MoveTo(transition.WorkStep,_timeSource.GetTime())))
            {
                throw new WipLimitViolationException(transition.WorkItem,transition.WorkStep);
            }
        }

        private void MoveToWithinParallelStep(WorkItemTransition transition)
        {
            var newTransition = transition;

            if (!WorkflowRepository.IsValidWorkStepForWorkItem(transition.WorkItem, transition.WorkStep))
            {
                newTransition = TraverseParallelMoveHierarchy(transition);
            }

            ThrowIfMovingToStepWithWrongClass(newTransition);

            CleanUpIfMovingFromExpandStep(newTransition);

            newTransition = AttemptMergeIfMovingChildOfParallelledWorkItem(newTransition);

            var resultTransition = DoMove(newTransition);

            TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(resultTransition);
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
            if (WorkflowRepository.IsChildOfParallelledWorkItem(transition.WorkItem, out parallelParent))
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
                var currentWorkStep = WorkflowRepository.GetWorkStep(inBetweenPath);
                var currentTransition = new WorkItemTransition(currentWorkItem,currentWorkStep);

                if (parentWorkStep != null && WorkflowRepository.IsParallelStep(parentWorkStep))
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

            var lockedAndMovedItem = transition.WorkItem
                .MoveTo(transition.WorkStep, _timeSource.GetTime())
                .UpdateStatus(WorkItemStatus.ParallelLocked);

            WorkflowRepository.UpdateWorkItem(lockedAndMovedItem);

            if (WorkflowRepository.IsInExpandStep(transition.WorkItem))
            {
                CleanUpIfMovingFromExpandStep(transition);
            }
        }

        private WorkItem CreateParallelledChildrenAndReturnWorkItemToMove(WorkItemTransition transition, WorkStep parallelStep)
        {
            var helper = new ParallelStepHelper(WorkflowRepository);

            var splitWorkItems = helper.SplitForParallelism(transition.WorkItem, parallelStep);

            foreach (var splitWorkItem in splitWorkItems)
            {
                WorkflowRepository.CreateWorkItem(splitWorkItem);

                WorkStep expandStep;
                var isInExpandStep = WorkflowRepository.IsInExpandStep(transition.WorkItem, out expandStep);
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
            if (!WorkflowRepository.IsExpandStep(transition.WorkStep) && !WorkflowRepository.IsWithinTransientStep(transition.WorkStep, out dummyStep) && WorkflowRepository.IsWithinExpandStep(transition.WorkStep))
            {
                throw new InvalidOperationException("Cannot move item to within expand step");
            }
        }

        private void ThrowIfMovingToStepWithWrongClass(WorkItemTransition transition)
        {
            if (!WorkflowRepository.IsValidWorkStepForWorkItem(transition.WorkItem, transition.WorkStep))
            {
                throw new InvalidOperationException("Invalid step for work item");
            }
        }

        private WorkItemTransition CreateTransitionIfMovingToExpandStep(WorkItemTransition transition)
        {
            if (WorkflowRepository.IsExpandStep(transition.WorkStep))
            {
                CreateTransientWorkSteps(transition.WorkItem, transition.WorkStep);
            }
            return transition;
        }

        private void CleanUpIfMovingFromExpandStep(WorkItemTransition transition)
        {
            var remover = new WorkItemRemover(WorkflowRepository);
            remover.CleanUpIfInExpandStep(transition.WorkItem);
        }

        private WorkItemTransition AttemptMergeIfMovingChildOfParallelledWorkItem(WorkItemTransition transition)
        {
            if (WorkflowRepository.IsMergeableParallelledChild(transition.WorkItem, transition.WorkStep))
            {
                var workItemToMove = MergeParallelWorkItems(transition);

                transition = new WorkItemTransition(workItemToMove, transition.WorkStep);
            }

            return transition;
        }

        private WorkItemTransition DoMove(WorkItemTransition transition)
        {
            var movedWorkItem = transition.WorkItem.MoveTo(transition.WorkStep,_timeSource.GetTime());

            WorkflowRepository.UpdateWorkItem(movedWorkItem);

            return new WorkItemTransition(movedWorkItem, transition.WorkStep);
        }

        private void TryUpdatingExpandLockIfMovingChildOfExpandedWorkItem(WorkItemTransition resultTransition)
        {
            if (WorkflowRepository.IsChildOfExpandedWorkItem(resultTransition.WorkItem))
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
            var transientWorkStep = WorkStep.New(transientRootPath).UpdateFrom(expandStep).UpdateType(WorkStepType.Transient).UpdateWorkItemClass(workItemClass);
            WorkflowRepository.CreateWorkStep(transientWorkStep);
        }

        private WorkItem MergeParallelWorkItems(WorkItemTransition transition)
        {
            var unlockedParentWorkItem = WorkflowRepository.GetWorkItem(transition.WorkItem.Parent.Id).UpdateStatus(WorkItemStatus.Normal);
            WorkflowRepository.UpdateWorkItem(unlockedParentWorkItem);

            foreach (var childWorkItem in WorkflowRepository.GetChildWorkItems(transition.WorkItem.Parent).ToList())
            {
                if (WorkflowRepository.IsExpandStep(transition.WorkStep))
                {
                    CleanUpIfMovingFromExpandStep(new WorkItemTransition(childWorkItem, transition.WorkStep));
                }

                WorkflowRepository.DeleteWorkItem(childWorkItem.Id);
            }

            return unlockedParentWorkItem;
        }

        private void TryUpdatingExpandLockOnParent(WorkItem item)
        {
            var parent = WorkflowRepository.GetWorkItem(item.Parent.Id);

            if (WorkflowRepository.GetChildWorkItems(item.Parent).All(WorkflowRepository.IsDone))
            {
                parent = parent.UpdateStatus(WorkItemStatus.Normal);
            }
            else
            {
                parent = parent.UpdateStatus(WorkItemStatus.ExpandLocked);
            }

            WorkflowRepository.UpdateWorkItem(parent);
        }

        private void CreateTransientWorkStepsRecursively(string transientRootPath, WorkStep rootStep, string workItemId)
        {
            var subSteps = WorkflowRepository.GetChildWorkSteps(rootStep.Path).Where(ws => ws.Type != WorkStepType.Transient);
            foreach (var childStep in subSteps)
            {
                var offset = childStep.Path.Remove(0, rootStep.Path.Length);

                var childTransientPath = transientRootPath + offset;

                var workItemClass = WorkItemClass.Combine(childStep.WorkItemClass, workItemId);
                WorkflowRepository.CreateWorkStep(WorkStep.New(childTransientPath).UpdateFrom(childStep).UpdateWorkItemClass(workItemClass));

                CreateTransientWorkStepsRecursively(childTransientPath, childStep, workItemId);
            }
        }
    }
}