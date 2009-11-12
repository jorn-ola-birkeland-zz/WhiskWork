using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhiskWork.Core
{
    public class WorkflowLogger : IWorkflow
    {
        private readonly IWorkItemLogger _logger;
        private readonly IWorkflow _workflow;

        public WorkflowLogger(IWorkItemLogger logger, IWorkflow workflow)
        {
            _logger = logger;
            _workflow = workflow;
        }

        public bool ExistsWorkItem(string id)
        {
            return _workflow.ExistsWorkItem(id);
        }

        public bool ExistsWorkStep(string path)
        {
            return _workflow.ExistsWorkStep(path);
        }

        public WorkItem GetWorkItem(string id)
        {
            return _workflow.GetWorkItem(id);
        }

        public void DeleteWorkItem(string workItemId)
        {
            _workflow.DeleteWorkItem(workItemId);
            _logger.LogDeleteWorkItem(workItemId);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            _workflow.CreateWorkStep(workStep);
            _logger.LogCreateWorkStep(workStep);
        }

        public void CreateWorkItem(WorkItem workItem)
        {
            _workflow.CreateWorkItem(workItem);
            _logger.LogCreateWorkItem(workItem);
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            var oldWorkItem = _workflow.GetWorkItem(workItem.Id);

            _workflow.UpdateWorkItem(workItem);
            _logger.LogUpdateWorkItem(oldWorkItem, workItem);
        }

    }
}
