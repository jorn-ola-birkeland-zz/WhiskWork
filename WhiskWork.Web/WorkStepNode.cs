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
            return new WorkStep(Combine(parentPath,_step),parentPath,_ordinal,_type, _workItemClass, _title);
        }

        private static string Combine(string path1, string path2)
        {
            path1 = path1.EndsWith("/") ? path1.Remove(path1.Length - 1, 1) : path1;
            path2 = path2.StartsWith("/") ? path2.Remove(0, 1) : path2;

            var result = path1 + "/" + path2;
            return result;
        }
    }
}