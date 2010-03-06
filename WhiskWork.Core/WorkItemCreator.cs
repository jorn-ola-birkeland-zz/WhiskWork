using System;
using System.Diagnostics;
using System.Linq;

namespace WhiskWork.Core
{
    internal class WorkItemCreator : WorkflowRepositoryInteraction
    {
        public WorkItemCreator(IWorkflowRepository workflowRepository) : base(workflowRepository)
        {
        }

        public void CreateWorkItem(WorkItem newWorkItem)
        {
            using(WorkflowRepository.BeginTransaction())
            {
                Create(newWorkItem);
                WorkflowRepository.CommitTransaction();                
            }
        }

        private void Create(WorkItem newWorkItem)
        {
            var leafStep = WorkflowRepository.GetLeafStep(newWorkItem.Path);

            if (leafStep.Type != WorkStepType.Begin)
            {
                throw new InvalidOperationException("Can only create work items in begin step");
            }

            var classes = WorkflowRepository.GetWorkItemClasses(leafStep);

            newWorkItem = newWorkItem.MoveTo(leafStep).UpdateClasses(classes);

            WorkStep transientStep;
            if (WorkflowRepository.IsWithinTransientStep(leafStep, out transientStep))
            {
                var parentItem = GetTransientParentWorkItem(transientStep);
                WorkflowRepository.UpdateWorkItem(parentItem.UpdateStatus(WorkItemStatus.ExpandLocked));

                newWorkItem = newWorkItem.MoveTo(leafStep).UpdateParent(parentItem,WorkItemParentType.Expanded);

                foreach (var workItemClass in newWorkItem.Classes)
                {
                    foreach (var rootClass in WorkItemClass.FindRootClasses(workItemClass))
                    {
                        newWorkItem = newWorkItem.AddClass(rootClass);
                    }
                }
            }
            else if (WorkflowRepository.IsWithinExpandStep(leafStep))
            {
                throw new InvalidOperationException("Cannot create item directly under expand step");
            }

            if (!newWorkItem.Ordinal.HasValue)
            {
                newWorkItem = newWorkItem.UpdateOrdinal(WorkflowRepository.GetNextOrdinal(newWorkItem));
            }

            WorkflowRepository.CreateWorkItem(newWorkItem);
        }

        private WorkItem GetTransientParentWorkItem(WorkStep transientStep)
        {
            var workItemId = transientStep.Path.Split(WorkflowPath.Separator).Last();

            return WorkflowRepository.GetWorkItem(workItemId);
        }
    }
}