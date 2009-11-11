using System.Collections.Specialized;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class WorkItemNode : IWorkflowNode
    {
        private readonly NameValueCollection _properties;
        private readonly string _id;
        private readonly int? _ordinal;
        public WorkItemNode(string id, int? ordinal, NameValueCollection properties)
        {
            _id = id;
            _ordinal = ordinal;
            _properties = properties;
        }

        public void AcceptVisitor(IWorkflowNodeVisitor visitor)
        {
            visitor.VisitWorkItem(this);
        }

        public WorkItem GetWorkItem(string path)
        {
            return WorkItem.NewUnchecked(_id, path, _ordinal, _properties);
        }
    }
}