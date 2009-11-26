using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class WorkStepNode : IWorkflowNode
    {
        private readonly string _step;
        private readonly int _ordinal;
        private readonly WorkStepType _type;
        private readonly string _workItemClass;
        private readonly string _title;

        public WorkStepNode(string step, int ordinal, WorkStepType type, string workItemClass, string title)
        {
            _step = step;
            _ordinal = ordinal;
            _type = type;
            _workItemClass = workItemClass;
            _title = title;
        }

        public void AcceptVisitor(IWorkflowNodeVisitor visitor)
        {
            visitor.VisitWorkStep(this);
        }

        public WorkStep GetWorkStep(string parentPath)
        {
            return new WorkStep(WorkflowPath.CombinePath(parentPath,_step),parentPath,_ordinal,_type, _workItemClass, _title);
        }

    }
}