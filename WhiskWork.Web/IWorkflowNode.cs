namespace WhiskWork.Web
{
    public interface IWorkflowNode
    {
        void AcceptVisitor(IWorkflowNodeVisitor visitor);
    }
}