using System;
using System.Collections.Generic;

namespace WhiskWork.Core.Logging
{
    public class WorkflowLogger : IWorkflow
    {
        private readonly IWorkflowLog _log;
        private readonly IWorkflow _workflow;

        public WorkflowLogger(IWorkflowLog log, IWorkflow workflow)
        {
            _log = log;
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

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return _workflow.GetWorkItems(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return _workflow.GetWorkStep(path);
        }

        public IEnumerable<WorkItem> GetChildWorkItems(WorkItemParent parent)
        {
            return _workflow.GetChildWorkItems(parent);
        }

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            return _workflow.GetChildWorkSteps(path);
        }

        public void DeleteWorkStep(string path)
        {
            _workflow.DeleteWorkStep(path);
        }

        public void UpdateWorkStep(WorkStep workStep)
        {
            _workflow.UpdateWorkStep(workStep);
        }

        public void MoveWorkStep(WorkStep movingWorkStep, WorkStep toStep)
        {
            _workflow.MoveWorkStep(movingWorkStep,toStep);
        }

        public void DeleteWorkItem(string workItemId)
        {
            var oldWorkItem = _workflow.GetWorkItem(workItemId);
            _workflow.DeleteWorkItem(workItemId);

            var entry = WorkItemLogEntry.DeleteEntry(oldWorkItem);

            _log.AddLogEntry(entry);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            var entry = WorkStepLogEntry.CreateEntry(workStep);
            _workflow.CreateWorkStep(workStep);
            _log.AddLogEntry(entry);
        }


        public void CreateWorkItem(WorkItem workItem)
        {
            var entry = WorkItemLogEntry.CreateEntry(workItem);
            _workflow.CreateWorkItem(workItem);
            _log.AddLogEntry(entry);
        }

        public void UpdateWorkItem(WorkItem workItem)
        {
            var oldWorkItem = _workflow.GetWorkItem(workItem.Id);

            var entry = WorkItemLogEntry.UpdateEntry(workItem, oldWorkItem);

            _workflow.UpdateWorkItem(workItem);
            _log.AddLogEntry(entry);
        }

    }
}