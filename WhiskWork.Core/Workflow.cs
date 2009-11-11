using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public class Workflow : WorkflowRepositoryInteraction, IWorkflow
    {
        public Workflow(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository) : base(workStepRepository, workItemRepository)
        {
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return WorkItemRepository.GetWorkItems(path).Where(wi => wi.Status != WorkItemStatus.ParallelLocked);
        }

        public void CreateWorkItem(WorkItem newWorkItem)
        {
            var creator = new WorkItemCreator(WorkStepRepository, WorkItemRepository);

            creator.CreateWorkItem(newWorkItem);
        }

        public void UpdateWorkItem(WorkItem changedWorkItem)
        {
            var currentWorkItem = GetWorkItemOrThrow(changedWorkItem.Id);

            currentWorkItem = currentWorkItem.UpdatePropertiesAndOrdinalFrom(changedWorkItem);

            var leafStep = WorkStepRepository.GetLeafStep(changedWorkItem.Path);

            if (currentWorkItem.Path != leafStep.Path)
            {
                var mover = new WorkItemMover(WorkStepRepository, WorkItemRepository);
                mover.MoveWorkItem(currentWorkItem, leafStep);
            }
            else
            {
                WorkItemRepository.UpdateWorkItem(currentWorkItem);
            }

        }

        public void DeleteWorkItem(string id)
        {
            var workItemRemover = new WorkItemRemover(WorkStepRepository, WorkItemRepository);
            workItemRemover.DeleteWorkItem(id);
        }


        public bool ExistsWorkItem(string workItemId)
        {
            return WorkItemRepository.ExistsWorkItem(workItemId);
        }

        public WorkItem GetWorkItem(string id)
        {
            return WorkItemRepository.GetWorkItem(id);
        }

        public bool ExistsWorkStep(string path)
        {
            return WorkStepRepository.ExistsWorkStep(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return WorkStepRepository.GetWorkStep(path);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            WorkStepRepository.CreateWorkStep(workStep);
        }

        private WorkItem GetWorkItemOrThrow(string workItemId)
        {
            WorkItem currentWorkItem;
            if (!WorkItemRepository.TryLocateWorkItem(workItemId, out currentWorkItem))
            {
                throw new ArgumentException("Work item was not found");
            }
            return currentWorkItem;
        }

    }
}