#region

using System;
using System.Linq;

#endregion

namespace WhiskWork.Core
{
    public class WorkStepUpdater : WorkflowRepositoryInteraction
    {
        public WorkStepUpdater(IWorkflowRepository workflowRepository) : base(workflowRepository)
        {
        }

        public void UpdateWorkStep(WorkStep workStepUpdate)
        {
            var currentWorkStep = WorkflowRepository.GetWorkStep(workStepUpdate.Path);

            ThrowIfUpdatingTypeAndNotEmpty(currentWorkStep, workStepUpdate, WorkStepType.Expand);
            ThrowIfUpdatingTypeAndNotEmpty(currentWorkStep, workStepUpdate, WorkStepType.Parallel);
            ThrowIfUpdatingToTransientStep(workStepUpdate);
            ThrowIfUpdatingTransientStep(currentWorkStep);
            ThrowIfASiblingHasSameOrdinal(workStepUpdate);
            ThrowIfUpdatingWipLimitAndWipLimitIsViolated(currentWorkStep, workStepUpdate);

            WorkflowRepository.UpdateWorkStep(currentWorkStep.UpdateFrom(workStepUpdate));
        }

        private void ThrowIfUpdatingWipLimitAndWipLimitIsViolated(WorkStep currentWorkStep, WorkStep workStepUpdate)
        {
            if(workStepUpdate.WipLimit!=null && (!currentWorkStep.WipLimit.HasValue || workStepUpdate.WipLimit!=currentWorkStep.WipLimit))
            {
                var checker = new WipLimitChecker(WorkflowRepository);
                if(checker.CountWip(currentWorkStep)>workStepUpdate.WipLimit)
                {
                    throw new InvalidOperationException("Cannot update wip limit below current work in process");
                }
            }
        }

        private void ThrowIfASiblingHasSameOrdinal(WorkStep workStep)
        {
            foreach (var childWorkStep in WorkflowRepository.GetChildWorkSteps(workStep.ParentPath))
            {
                if(childWorkStep.Path ==workStep.Path)
                {
                    continue;
                }

                if(childWorkStep.Ordinal==workStep.Ordinal)
                {
                    throw new InvalidOperationException("Cannot update to conflicting ordinal");
                }
            }
        }

        private static void ThrowIfUpdatingTransientStep(WorkStep workStep)
        {
            if (workStep.Type == WorkStepType.Transient)
            {
                throw new InvalidOperationException("Cannot update transient step");
            }
        }

        private static void ThrowIfUpdatingToTransientStep(WorkStep workStepUpdate)
        {
            if(workStepUpdate.Type==WorkStepType.Transient)
            {
                throw new InvalidOperationException("Cannot update to transient step");
            }
        }

        private void ThrowIfUpdatingTypeAndNotEmpty(WorkStep currentWorkStep, WorkStep workStepUpdate,
                                                    WorkStepType workStepType)
        {
            if ((currentWorkStep.Type == workStepType && workStepUpdate.Type != workStepType) ||
                (currentWorkStep.Type != workStepType && workStepUpdate.Type == workStepType))
            {
                if (WorkflowRepository.GetChildWorkSteps(workStepUpdate.Path).Count() > 0)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot update workstep type from {0} to {1} workstep when it has children",
                                      currentWorkStep.Type, workStepUpdate.Type));
                }

                if (WorkflowRepository.GetWorkItems(workStepUpdate.Path).Count() > 0)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot update workstep type from {0} to {1} when it has work items",
                                      currentWorkStep.Type, workStepUpdate.Type));
                }
            }
        }
    }
}