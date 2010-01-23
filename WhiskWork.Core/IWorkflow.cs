namespace WhiskWork.Core
{
    public interface IWorkflow : IReadableWorkflowRepository, IWriteableWorkflowRepository
    {
        void MoveWorkStep(WorkStep movingWorkStep, WorkStep toStep);
    }

    public interface IWorkflowRepository : IReadableWorkflowRepository, IWriteableWorkflowRepository
    {
    }


    public interface IReadableWorkflowRepository : IReadableWorkItemRepository, IReadableWorkStepRepository
    {
        
    }

    public interface IWriteableWorkflowRepository : IWriteableWorkItemRepository, IWriteableWorkStepRepository
    {
        
    }
}