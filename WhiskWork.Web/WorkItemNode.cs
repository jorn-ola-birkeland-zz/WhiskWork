using System;
using System.Collections.Specialized;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class WorkItemNode : IWorkflowNode
    {
        private readonly NameValueCollection _properties;
        private readonly string _id;
        private readonly int? _ordinal;
        private readonly DateTime? _timeStamp;
        public WorkItemNode(string id, int? ordinal, DateTime? timeStamp, NameValueCollection properties)
        {
            _id = id;
            _ordinal = ordinal;
            _properties = properties;
            _timeStamp = timeStamp;
        }

        public void AcceptVisitor(IWorkflowNodeVisitor visitor)
        {
            visitor.VisitWorkItem(this);
        }

        public WorkItem GetWorkItem(string path)
        {
            return WorkItem.NewUnchecked(_id, path, _ordinal, _timeStamp, _properties);
        }
    }
}