using System.Collections.Generic;

namespace WhiskWork.Core
{
    public interface IReadableWorkStepRepository
    {
        IEnumerable<WorkStep> GetChildWorkSteps(string path);
        WorkStep GetWorkStep(string path);
        bool ExistsWorkStep(string path);
    }

    public interface IWriteableWorkStepRepository
    {
        void CreateWorkStep(WorkStep workStep);
        void DeleteWorkStep(string path);
        void UpdateWorkStep(WorkStep workStep);
    }

    public interface IWorkStepRepository: IReadableWorkStepRepository, IWriteableWorkStepRepository
    {
    }

    public interface ICacheableWorkStepRepository : IWorkStepRepository
    {
        IEnumerable<WorkStep> GetAllWorkSteps();
    }
}