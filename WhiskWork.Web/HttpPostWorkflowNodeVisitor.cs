using System;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class HttpPostWorkflowNodeVisitor : IWorkflowNodeVisitor
    {
        private readonly string _path;
        private readonly Workflow _wp;

        public HttpPostWorkflowNodeVisitor(Workflow wp, string path)
        {
            _path = path;
            _wp = wp;
        }

        public void VisitWorkStep(WorkStepNode workStepNode)
        {
            if (_path!=RootWorkStep.Instance.Path && !_wp.ExistsWorkStep(_path))
            {
                Response = WorkflowHttpResponse.NotFound;
            }

            var workStep = workStepNode.GetWorkStep(_path);

            try
            {
                _wp.CreateWorkStep(workStep);
            }
            catch (Exception e)
            {
                Response = WorkflowHttpResponse.Forbidden;
            }

            Response = WorkflowHttpResponse.Created(workStep.Path);
        }

        public void VisitWorkItem(WorkItemNode workItemNode)
        {
            if (!_wp.ExistsWorkStep(_path))
            {
                Response = WorkflowHttpResponse.NotFound;
            }

            var workItem = workItemNode.GetWorkItem(_path);

            try
            {
                _wp.CreateWorkItem(workItem);
            }
            catch (Exception e)
            {
                Response = WorkflowHttpResponse.Forbidden;
            }

            Response = WorkflowHttpResponse.Created(_path + "/" + workItem.Id);
        }

        public WorkflowHttpResponse Response { get; private set; }
    }
}