#region

using System;
using System.Collections.Generic;

#endregion

namespace WhiskWork.Core
{
    public class Workflow : WorkflowRepositoryInteraction, IWorkflow
    {
        public Workflow(IWorkflowRepository workflowRepository) : base(workflowRepository)
        {
            TimeSource = new DefaultTimeSource();
        }

        public ITimeSource TimeSource { get; set; }

        #region IWorkflow Members

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return WorkflowRepository.GetWorkItems(path);
        }

        public void CreateWorkItem(WorkItem newWorkItem)
        {
            var creator = new WorkItemCreator(WorkflowRepository);
            
            
            var timeStamp = TimeSource.GetTime();
            creator.CreateWorkItem(newWorkItem.UpdateTimestamp(timeStamp).UpdateLastMoved(timeStamp));
        }

        public void UpdateWorkItem(WorkItem changedWorkItem)
        {
            var currentWorkItem = GetWorkItemOrThrow(changedWorkItem.Id);
            
            ThrowIfConflictingTimestamp(currentWorkItem, changedWorkItem);

            currentWorkItem =
                currentWorkItem.UpdatePropertiesAndOrdinalFrom(changedWorkItem).UpdateTimestamp(TimeSource.GetTime());

            var leafStep = WorkflowRepository.GetLeafStep(changedWorkItem.Path);

            if (changedWorkItem.Path == currentWorkItem.Path || currentWorkItem.Path == leafStep.Path)
            {
                WorkflowRepository.UpdateWorkItem(currentWorkItem);
            }
            else
            {
                var mover = new WorkItemMover(WorkflowRepository,TimeSource);
                mover.MoveWorkItem(currentWorkItem, leafStep);
            }
        }

        public void DeleteWorkItem(string id)
        {
            var workItemRemover = new WorkItemRemover(WorkflowRepository);
            workItemRemover.DeleteWorkItem(id);
        }


        public bool ExistsWorkItem(string workItemId)
        {
            return WorkflowRepository.ExistsWorkItem(workItemId);
        }

        public WorkItem GetWorkItem(string id)
        {
            return WorkflowRepository.GetWorkItem(id);
        }

        public bool ExistsWorkStep(string path)
        {
            return WorkflowRepository.ExistsWorkStep(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return WorkflowRepository.GetWorkStep(path);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            var creator = new WorkStepCreator(WorkflowRepository);
            creator.CreateWorkStep(workStep);
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            return WorkflowRepository.GetChildWorkItems(parent);
        }

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            return WorkflowRepository.GetChildWorkSteps(path);
        }

        public void DeleteWorkStep(string path)
        {
            WorkflowRepository.DeleteWorkStep(path);
        }

        public void UpdateWorkStep(WorkStep workStep)
        {
            var updater = new WorkStepUpdater(WorkflowRepository);
            updater.UpdateWorkStep(workStep);
        }

        public void MoveWorkStep(WorkStep stepToMove, WorkStep toStep)
        {
            var mover = new WorkStepMover(WorkflowRepository);
            mover.MoveWorkStep(stepToMove, toStep);
        }

        #endregion

        private static void ThrowIfConflictingTimestamp(WorkItem currentWorkItem, WorkItem changedWorkItem)
        {
            if (changedWorkItem.Timestamp.HasValue && changedWorkItem.Timestamp.Value != currentWorkItem.Timestamp.Value)
            {
                throw new InvalidOperationException("Conflicting timestamps");
            }
        }

        private WorkItem GetWorkItemOrThrow(string workItemId)
        {
            WorkItem currentWorkItem;
            if (!WorkflowRepository.TryLocateWorkItem(workItemId, out currentWorkItem))
            {
                throw new ArgumentException("Work item was not found");
            }
            return currentWorkItem;
        }
    }

    internal class DefaultTimeSource : ITimeSource
    {
        #region ITimeSource Members

        public DateTime GetTime()
        {
            return DateTime.Now;
        }

        #endregion
    }
}