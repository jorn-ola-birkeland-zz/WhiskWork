using System;
using System.Collections.Specialized;
using System.Linq;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class HttpPutWorkflowNodeVisitor : IWorkflowNodeVisitor
    {
        private readonly string _path;
        private readonly Workflow _wp;
        
        public HttpPutWorkflowNodeVisitor(Workflow workflow, string path)
        {
            _path = path;
            _wp = workflow;
        }

        public void VisitWorkStep(WorkStepNode workStepNode)
        {
            throw new NotImplementedException();
        }

        public void VisitWorkItem(WorkItemNode workItemNode)
        {
            string path;
            string workItemId;

            var potentialWorkItemId = _path.Split('/').Last();

            if (_wp.ExistsWorkItem(potentialWorkItemId))
            {
                workItemId = potentialWorkItemId;
                path = _path.Substring(0, _path.LastIndexOf('/'));
            }
            else
            {
                Response = WorkflowHttpResponse.Forbidden;
                return;
            }

            if (!_wp.ExistsWorkItem(workItemId) || !_wp.ExistsWorkStep(path))
            {
                Response = WorkflowHttpResponse.NotFound;
                return;
            }

            WorkItem updatdWorkItem;
            try
            {
                updatdWorkItem = _wp.UpdateWorkItem(workItemId, path, new NameValueCollection());
            }
            catch (Exception)
            {
                Response = WorkflowHttpResponse.Forbidden;
                return;
            }

            Response = updatdWorkItem.Path != path
                           ? WorkflowHttpResponse.MovedPermanently(updatdWorkItem.Path + "/" + workItemId)
                           : WorkflowHttpResponse.Ok;
        }

        public WorkflowHttpResponse Response { get; private set; }
    }
}