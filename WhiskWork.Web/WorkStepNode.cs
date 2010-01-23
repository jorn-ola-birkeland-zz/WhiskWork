using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class WorkStepNode : IWorkflowNode
    {
        public WorkStepNode()
        {
        }

        public string Step { get; set; }
        public int? Ordinal { get; set; }
        public WorkStepType? Type { get; set; }
        public string WorkItemClass { get; set; }
        public string Title { get; set; }

        public void AcceptVisitor(IWorkflowNodeVisitor visitor)
        {
            visitor.VisitWorkStep(this);
        }

        public WorkStep GetWorkStep(string parentPath)
        {
            var workStep = WorkStep.New(WorkflowPath.CombinePath(parentPath,Step));
            if(Ordinal.HasValue)
            {
                workStep = workStep.UpdateOrdinal(Ordinal.Value);
            }

            if(Type.HasValue)
            {
                workStep = workStep.UpdateType(Type.Value);
            }

            if(WorkItemClass!=null)
            {
                workStep = workStep.UpdateWorkItemClass(WorkItemClass);
            }

            if(Title!=null)
            {
                workStep = workStep.UpdateTitle(Title);
            }

            return workStep;
        }
    }
}