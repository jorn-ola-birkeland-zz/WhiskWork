using WhiskWork.Core;

namespace WhiskWork.CommondLineWebServer
{
    internal interface IWorkflowRepositoryFactory
    {
        IWorkflowRepository WorkflowRepository { get; }
    }
}