namespace WhiskWork.Web
{
    internal interface IWorkflowNode
    {
        void AcceptVisitor(IWorkflowNodeVisitor visitor);
    }
}