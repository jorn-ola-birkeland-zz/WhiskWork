using System;
using System.Diagnostics;
using System.Linq;

namespace WhiskWork.Core
{
    internal class WorkItemCreator : WorkflowRepositoryInteraction
    {
        public WorkItemCreator(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository) : base(workStepRepository, workItemRepository)
        {
        }

        public void CreateWorkItem(WorkItem newWorkItem)
        {
            var leafStep = WorkStepRepository.GetLeafStep(newWorkItem.Path);

            if (leafStep.Type != WorkStepType.Begin)
            {
                throw new InvalidOperationException("Can only create work items in begin step");
            }

            var classes = WorkStepRepository.GetWorkItemClasses(leafStep);

            newWorkItem = newWorkItem.MoveTo(leafStep).ReplacesClasses(classes);

            WorkStep transientStep;
            if (WorkStepRepository.IsWithinTransientStep(leafStep, out transientStep))
            {
                WorkItem parentItem = GetTransientParentWorkItem(transientStep);
                WorkItemRepository.UpdateWorkItem(parentItem.UpdateStatus(WorkItemStatus.ExpandLocked));

                newWorkItem = newWorkItem.MoveTo(leafStep).UpdateParent(parentItem);

                foreach (var workItemClass in newWorkItem.Classes)
                {
                    foreach (var rootClass in WorkItemClass.FindRootClasses(workItemClass))
                    {
                        newWorkItem = newWorkItem.AddClass(rootClass);
                    }
                }
            }
            else if (WorkStepRepository.IsWithinExpandStep(leafStep))
            {
                throw new InvalidOperationException("Cannot create item directly under expand step");
            }

            if (!newWorkItem.HasOrdinal)
            {
                newWorkItem = newWorkItem.UpdateOrdinal(WorkItemRepository.GetNextOrdinal(newWorkItem));
            }

            WorkItemRepository.CreateWorkItem(newWorkItem);
        }

        private WorkItem GetTransientParentWorkItem(WorkStep transientStep)
        {
            var workItemId = transientStep.Path.Split(WorkStep.Separator).Last();

            return WorkItemRepository.GetWorkItem(workItemId);
        }
    }
}