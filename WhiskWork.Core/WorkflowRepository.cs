using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhiskWork.Core
{
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly IWorkItemRepository _workItemRepsitory;
        private readonly IWorkStepRepository _workStepRepository;

        public WorkflowRepository(IWorkItemRepository workItemRepsitory, IWorkStepRepository workStepRepository)
        {
            _workItemRepsitory = workItemRepsitory;
            _workStepRepository = workStepRepository;
        }

        public bool ExistsWorkItem(string id)
        {
            return _workItemRepsitory.ExistsWorkItem(id);
        }

        public WorkItem GetWorkItem(string id)
        {
            return _workItemRepsitory.GetWorkItem(id);
        }

        public void CreateWorkItem(WorkItem workItem)
        {
            _workItemRepsitory.CreateWorkItem(workItem);
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return _workItemRepsitory.GetWorkItems(path);
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            _workItemRepsitory.UpdateWorkItem(workItem);
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            return _workItemRepsitory.GetChildWorkItems(parent);
        }

        public void DeleteWorkItem(string workItemId)
        {
            _workItemRepsitory.DeleteWorkItem(workItemId);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            _workStepRepository.CreateWorkStep(workStep);
        }

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            return _workStepRepository.GetChildWorkSteps(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return _workStepRepository.GetWorkStep(path);
        }

        public void DeleteWorkStep(string path)
        {
            _workStepRepository.DeleteWorkStep(path);
        }

        public void UpdateWorkStep(WorkStep workStep)
        {
            _workStepRepository.UpdateWorkStep(workStep);
        }

        public bool ExistsWorkStep(string path)
        {
            return _workStepRepository.ExistsWorkStep(path);
        }
    }
}
