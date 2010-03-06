using System;
namespace WhiskWork.Core
{
    public interface IWorkflow : IReadableWorkflowRepository, IWriteableWorkflowRepository
    {
        void MoveWorkStep(WorkStep movingWorkStep, WorkStep toStep);
    }

    public interface IWorkflowRepository : IReadableWorkflowRepository, IWriteableWorkflowRepository
    {
        IDisposable BeginTransaction();
        void CommitTransaction();
    }


    public interface IReadableWorkflowRepository : IReadableWorkItemRepository, IReadableWorkStepRepository
    {
        
    }

    public interface IWriteableWorkflowRepository : IWriteableWorkItemRepository, IWriteableWorkStepRepository
    {
        
    }
}