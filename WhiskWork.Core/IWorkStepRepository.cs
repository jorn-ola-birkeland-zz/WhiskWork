using System.Collections.Generic;

namespace WhiskWork.Core
{
    public interface IWorkStepRepository
    {
        void CreateWorkStep(WorkStep workStep);
        IEnumerable<WorkStep> GetChildWorkSteps(string path);
        WorkStep GetWorkStep(string path);

        void DeleteWorkStep(string path);
        bool ExistsWorkStep(string path);
    }
}