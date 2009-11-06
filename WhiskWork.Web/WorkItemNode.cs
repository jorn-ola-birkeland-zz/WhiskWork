using System.Collections.Specialized;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class WorkItemNode : IWorkflowNode
    {
        private readonly NameValueCollection _properties;
        private readonly string _id; 
        public WorkItemNode(string id, NameValueCollection properties)
        {
            _id = id;
            _properties = properties;
        }

        public void AcceptVisitor(IWorkflowNodeVisitor visitor)
        {
            visitor.VisitWorkItem(this);
        }

        public WorkItem GetWorkItem(string path)
        {
            return WorkItem.NewUnchecked(_id, path, _properties);
        }
    }
}