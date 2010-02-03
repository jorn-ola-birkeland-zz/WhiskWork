using System;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class HttpPostWorkflowNodeVisitor : IWorkflowNodeVisitor
    {
        private readonly string _path;
        private readonly IWorkflow _workflow;

        public HttpPostWorkflowNodeVisitor(IWorkflow wp, string path)
        {
            _path = path;
            _workflow = wp;
        }

        public WorkflowHttpResponse Response { get; private set; }

        #region IWorkflowNodeVisitor Members

        public void VisitWorkStep(WorkStepNode workStepNode)
        {
            if (_path != WorkStep.Root.Path && !_workflow.ExistsWorkStep(_path))
            {
                Response = WorkflowHttpResponse.NotFound;
            }


            TryOperation(
                () =>
                    {
                        WorkStep workStep = workStepNode.GetWorkStep(_path);
                        _workflow.CreateWorkStep(workStep);
                        Response = WorkflowHttpResponse.Created(workStep.Path);
                    }
                );
        }

        public void VisitWorkItem(WorkItemNode workItemNode)
        {
            if (!_workflow.ExistsWorkStep(_path))
            {
                Response = WorkflowHttpResponse.NotFound;
            }

            TryOperation(
                () =>
                    {
                        HandleWorkItem(workItemNode);
                    }
                );
        }

        private void HandleWorkItem(WorkItemNode workItemNode)
        {
            var workItem = workItemNode.GetWorkItem(_path);

            if (!_workflow.ExistsWorkItem(workItem.Id))
            {
                _workflow.CreateWorkItem(workItem);
                Response = WorkflowHttpResponse.Created(workItem.Path);
            }
            else
            {
                _workflow.UpdateWorkItem(workItem);
                Response = WorkflowHttpResponse.Ok;
            }
        }

        #endregion

        private void TryOperation(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (ArgumentException e)
            {
                Response = WorkflowHttpResponse.BadRequest(e);
            }
            catch (InvalidOperationException e)
            {
                Response = WorkflowHttpResponse.Forbidden(e);
            }
            catch (Exception e)
            {
                Response = WorkflowHttpResponse.InternalServerError(e);
            }
        }
    }
}