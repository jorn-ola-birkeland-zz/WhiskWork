using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WipLimitTest
    {
        private WipLimitChecker _wipLimit;
        IWorkStepRepository _workStepRepository;
        IWorkItemRepository _workItemRepository;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _wipLimit = new WipLimitChecker(_workStepRepository,_workItemRepository);
        }

        [TestMethod]
        public void ShouldNotAcceptNewWorkItemIfNumberOfWorkItemsInWorkStepIsEqualToWipLimit()
        {
            var analysis = WorkStep.New("/analysis").UpdateWipLimit(2);
            _workStepRepository.CreateWorkStep(analysis);

            AssertBelowWipLimitAndCreateWorkItem(analysis, "cr1");
            AssertBelowWipLimitAndCreateWorkItem(analysis, "cr2");


            Assert.IsFalse(CanAcceptWip(analysis,"cr3"));
        }

        [TestMethod]
        public void ShouldNotAcceptWorkItemsIfNumberOfWorkItemsSubstepsIsEqualToWipLimit()
        {
            var analysis = _workStepRepository.CreateWorkStep("/analysis", 2);
            var analysisInProcess = _workStepRepository.CreateWorkStep("/analysis/inprocess");
            var analysisDone = _workStepRepository.CreateWorkStep("/analysis/done");

            AssertBelowWipLimitAndCreateWorkItem(analysisInProcess, "cr1");
            AssertBelowWipLimitAndCreateWorkItem(analysisDone, "cr2");

            Assert.IsFalse(CanAcceptWip(analysis, "cr3"));
            Assert.IsFalse(CanAcceptWip(analysisDone,"cr3"));
            Assert.IsFalse(CanAcceptWip(analysisInProcess,"cr3"));
        }

        [TestMethod]
        public void ShouldRespectSubTreeWipLimits()
        {
            _workStepRepository.CreateWorkStep("/analysis", 3);
            var analysisInProcess = _workStepRepository.CreateWorkStep("/analysis/inprocess", 1);
            var analysisDone = _workStepRepository.CreateWorkStep("/analysis/done");

            AssertBelowWipLimitAndCreateWorkItem(analysisInProcess, "cr1");
            AssertBelowWipLimitAndCreateWorkItem(analysisDone, "cr2");

            Assert.IsFalse(CanAcceptWip(analysisInProcess, "cr3"));
            AssertBelowWipLimitAndCreateWorkItem(analysisDone, "cr3");

            Assert.IsFalse(CanAcceptWip(analysisDone, "cr4"));
        }

        [TestMethod]
        public void ShouldNotCountWipBelowParallelStepWhenCheckingAboveParallelStep()
        {
            _workStepRepository.CreateWorkStep("/wip", 2);
            var feedback = _workStepRepository.CreateWorkStep("/wip/feedback", WorkStepType.Parallel);
            var review = _workStepRepository.CreateWorkStep("/wip/feedback/review");

            _workItemRepository.CreateWorkItem(review, "cr1");
            AssertBelowWipLimitAndCreateWorkItem(feedback, "cr4", "cr5");
            Assert.IsFalse(CanAcceptWip(feedback,"cr6"));

        }

        [TestMethod]
        public void ShouldNotCountWipAboveAndIncludingParallelStepWhenCheckingBelowParallelStep()
        {
            _workStepRepository.CreateWorkStep("/wip", 2);
            _workStepRepository.CreateWorkStep("/wip/feedback", WorkStepType.Parallel);
            var review = _workStepRepository.CreateWorkStep("/wip/feedback/review");

            AssertBelowWipLimitAndCreateWorkItem(review, "cr1", "cr2");
            Assert.IsTrue(CanAcceptWip(review,"cr3"));
        }

        [TestMethod]
        public void ShouldNotCountWipBelowTransientStepWhenCheckingAboveTransientStep()
        {
            _workStepRepository.CreateWorkStep("/dev", 2);
            var devInProcess = _workStepRepository.CreateWorkStep("/dev/inprocess", WorkStepType.Transient);
            var tasks = _workStepRepository.CreateWorkStep("/dev/inprocess/tasks");

            _workItemRepository.CreateWorkItem(tasks, "task1");
            AssertBelowWipLimitAndCreateWorkItem(devInProcess, "cr1", "cr2");
            Assert.IsFalse(CanAcceptWip(devInProcess, "cr3"));
        }
        
        [TestMethod]
        public void ShouldNotCountWipAboveAndIncludingTransientStepWhenCheckingBelowTransientStep()
        {
            _workStepRepository.CreateWorkStep("/dev", 2);
            _workStepRepository.CreateWorkStep("/dev/inprocess", WorkStepType.Transient);
            var tasks = _workStepRepository.CreateWorkStep("/dev/inprocess/tasks");

            AssertBelowWipLimitAndCreateWorkItem(tasks, "tasks1", "tasks2");
            Assert.IsTrue(CanAcceptWip(tasks, "task3"));
        }

        [TestMethod]
        public void ShouldNotCountWorkItemBeingMoved()
        {
            _workStepRepository.CreateWorkStep("/analysis", 2);
            var analysisInProcess = _workStepRepository.CreateWorkStep("/analysis/inprocess");
            var analysisDone = _workStepRepository.CreateWorkStep("/analysis/done");

            AssertBelowWipLimitAndCreateWorkItem(analysisInProcess, "cr1");
            AssertBelowWipLimitAndCreateWorkItem(analysisDone, "cr2");

            Assert.IsFalse(CanAcceptWip(analysisDone, "cr3"));
            Assert.IsTrue(CanAcceptWip(analysisDone, "cr1"));
        }

        [TestMethod]
        public void ShouldNotAcceptWorkStepWithWorkItemsViolatingWipLimit()
        {
           var ws1 = _workStepRepository.CreateWorkStep("/step1", 1);
           var ws2 =  _workStepRepository.CreateWorkStep("/step2");

            _workItemRepository.CreateWorkItem("/step2", "wi1","wi2");

            Assert.IsFalse(_wipLimit.CanAcceptWorkStep(ws1, ws2));
        }

        [TestMethod]
        public void ShouldAcceptWorkStepWithWorkItemsRespectingWipLimit()
        {
            var ws1 = _workStepRepository.CreateWorkStep("/step1", 2);
            var ws2 = _workStepRepository.CreateWorkStep("/step2");

            _workItemRepository.CreateWorkItem("/step2", "wi1", "wi2");

            Assert.IsTrue(_wipLimit.CanAcceptWorkStep(ws1, ws2));
        }



        private void AssertBelowWipLimitAndCreateWorkItem(WorkStep workStep, params string[] ids)
        {
            foreach (var id in ids)
            {
                Assert.IsTrue(CanAcceptWip(workStep, id));
                _workItemRepository.CreateWorkItem(workStep, id);
            }
        }


        private bool CanAcceptWip(WorkStep workStep, string workItemId)
        {
            return _wipLimit.CanAcceptWorkItem(WorkItem.New(workItemId, workStep.Path));
        }
    }
}
