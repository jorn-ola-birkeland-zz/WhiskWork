using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core.Exception;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkItemMoverTest
    {
        MemoryWorkStepRepository _workStepRepository;
        MemoryWorkItemRepository _workItemRepository;
        private Workflow _workflow;
        private MockRepository _mocks;
        WorkItemMover _mover;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            var repository = new WorkflowRepository(_workItemRepository, _workStepRepository);
            _workflow = new Workflow(repository);

            _mover = new WorkItemMover(repository);
            _mocks = new MockRepository();
        }

        [TestMethod]
        public void ShouldThrowExceptionIfWipLimitWillBeViolated()
        {
            var wipLimitChecker = _mocks.Stub<IWipLimitChecker>();

            _mover.WipLimitChecker = wipLimitChecker;

            var analysisStep = WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr");
            var devStep = WorkStep.New("/dev").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr");

            _workStepRepository.Add(analysisStep);
            _workStepRepository.Add(devStep);

            _workflow.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            var workItem = _workItemRepository.GetWorkItem("cr1");

            SetupResult.For(wipLimitChecker.CanAcceptWorkItem(null)).IgnoreArguments().Return(false);

            AssertUtils.AssertThrows<WipLimitViolationException>(
                () => _mover.MoveWorkItem(workItem,devStep)
                );
        }

    }
}
