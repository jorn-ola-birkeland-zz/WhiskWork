using WhiskWork.Core;

namespace WhiskWork.Web
{
    public interface IWorkflowNodeVisitor
    {
        void VisitWorkStep(WorkStepNode workStepNode);
        void VisitWorkItem(WorkItemNode workItemNode);
    }
}