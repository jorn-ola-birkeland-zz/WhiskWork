#region

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Test.Common;

#endregion

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkStepMoverTest
    {
        private WorkStepMover _mover;
        private IWorkflowRepository _wr;
        private MockRepository _mocks;
        private ITimeSource _timeSourceStub;

        [TestInitialize]
        public void Init()
        {
            _mocks = new MockRepository();
            _wr = new WorkflowRepository(new MemoryWorkItemRepository(), new MemoryWorkStepRepository());
            _timeSourceStub = _mocks.Stub<ITimeSource>();
            _mover = new WorkStepMover(_wr,_timeSourceStub);
        }

        [TestMethod]
        public void ShouldMoveWorkStepsWithoutWorkItems()
        {
            _wr.CreateWorkStep("/dev");
            var devInProcess = _wr.CreateWorkStep("/dev/inprocess");
            var devInProcessTasks = _wr.CreateWorkStep("/dev/inprocess/tasks");
            var dev2 = _wr.CreateWorkStep("/dev2");

            _mover.MoveWorkStep(devInProcess, dev2);

            var dev2Child = _wr.GetChildWorkSteps(dev2.Path).Single();
            Assert.AreEqual("/dev2/inprocess", dev2Child.Path);

            var dev2InprocessChild = _wr.GetChildWorkSteps("/dev2/inprocess").Single();
            Assert.AreEqual("/dev2/inprocess/tasks", dev2InprocessChild.Path);

            Assert.IsFalse(_wr.ExistsWorkStep(devInProcess.Path));
            Assert.IsFalse(_wr.ExistsWorkStep(devInProcessTasks.Path));
        }

        [TestMethod]
        public void ShouldMoveWorkStepsWithWorkItems()
        {
            var dev = _wr.CreateWorkStep("/dev");
            var devInProcess = _wr.CreateWorkStep("/dev/inprocess");
            var devInProcessTasks = _wr.CreateWorkStep("/dev/inprocess/tasks");
            var dev2 = _wr.CreateWorkStep("/dev2");

            _wr.CreateWorkItem(dev, "cr1");
            _wr.CreateWorkItem(devInProcess, "cr2");
            _wr.CreateWorkItem(devInProcessTasks, "task1");
            _wr.CreateWorkItem(dev2, "cr3");

            _mover.MoveWorkStep(devInProcess, dev2);

            Assert.AreEqual(dev.Path, _wr.GetWorkItem("cr1").Path);
            Assert.AreEqual("/dev2/inprocess", _wr.GetWorkItem("cr2").Path);
            Assert.AreEqual("/dev2/inprocess/tasks", _wr.GetWorkItem("task1").Path);
            Assert.AreEqual(dev2.Path, _wr.GetWorkItem("cr3").Path);
        }

        [TestMethod]
        public void ShouldThrowExceptionIfMovingWorkStepToStepWithIncompatibleWorkItemClass()
        {
            var analysis = WorkStep.New("/analysis").UpdateWorkItemClass("cr");
            var dev = WorkStep.New("/development").UpdateWorkItemClass("task");

            _wr.CreateWorkStep(analysis);
            _wr.CreateWorkStep(dev);

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(dev, analysis)
                );
        }

        [TestMethod]
        public void ShouldThrowExceptionWhenMovingWorkStepAndNewSiblingHasSameOrdinal()
        {
            var dev = _wr.CreateWorkStep("/dev", 1);
            _wr.CreateWorkStep("/dev/inprocess", 1);
            var devInProcessTasks = _wr.CreateWorkStep("/dev/inprocess/tasks", 1);

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(devInProcessTasks, dev)
                );
        }

        [TestMethod]
        public void ShouldNotMoveDecendantOfExpandStepOutsideExpandStep()
        {
            var analysis = _wr.CreateWorkStep("/analysis");
            _wr.CreateWorkStep("/dev", WorkStepType.Expand);
            var devInProcess = _wr.CreateWorkStep("/dev/inprocess");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(devInProcess, analysis)
                );
        }

        [TestMethod]
        public void ShouldMoveDecendantOfExpandStepWithinExpandStep()
        {
            _wr.CreateWorkStep("/expand", WorkStepType.Expand);
            var sub1 = _wr.CreateWorkStep("/expand/sub1");
            var sub2 = _wr.CreateWorkStep("/expand/sub2");

            _mover.MoveWorkStep(sub2, sub1);
            Assert.AreEqual("/expand/sub1/sub2", _wr.GetWorkStep("/expand/sub1/sub2").Path);
        }

        [TestMethod]
        public void ShouldMoveExpandStep()
        {
            _wr.CreateWorkStep("/step1");
            var step2 = _wr.CreateWorkStep("/step2");
            var step1Expand = _wr.CreateWorkStep("/step1/expand", WorkStepType.Expand);

            _mover.MoveWorkStep(step1Expand, step2);
            Assert.AreEqual("/step2/expand", _wr.GetWorkStep("/step2/expand").Path);
        }

        [TestMethod]
        public void ShouldNotMoveDecendantOfParallelStepOutsideParallelStep()
        {
            var analysis = _wr.CreateWorkStep("/analysis");
            _wr.CreateWorkStep("/dev", WorkStepType.Parallel);
            var devInProcess = _wr.CreateWorkStep("/dev/inprocess");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(devInProcess, analysis)
                );
        }

        [TestMethod]
        public void ShouldMoveDecendantOfParallelStepWithinParallelStep()
        {
            _wr.CreateWorkStep("/expand", WorkStepType.Expand);
            var sub1 = _wr.CreateWorkStep("/expand/sub1");
            var sub2 = _wr.CreateWorkStep("/expand/sub2");

            _mover.MoveWorkStep(sub2, sub1);
            Assert.AreEqual("/expand/sub1/sub2", _wr.GetWorkStep("/expand/sub1/sub2").Path);
        }

        [TestMethod]
        public void ShouldMoveParallelStep()
        {
            _wr.CreateWorkStep("/step1");
            var step2 = _wr.CreateWorkStep("/step2");
            var step1Parallel = _wr.CreateWorkStep("/step1/expand", WorkStepType.Parallel);

            _mover.MoveWorkStep(step1Parallel, step2);
            Assert.AreEqual("/step2/expand", _wr.GetWorkStep("/step2/expand").Path);
        }

        [TestMethod]
        public void ShouldNotMoveIfWipLimitOfNewParentIsViolated()
        {
            _wr.CreateWorkStep("/step1");
            var step2 = _wr.CreateWorkStep("/step2", 1);
            var step1Sub = _wr.CreateWorkStep("/step1/subStep");

            _wr.CreateWorkItem("/step1/subStep", "cr1", "cr2");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(step1Sub, step2)
                );
        }


        [TestMethod]
        public void ShouldNotMoveTransientStep()
        {
            _wr.CreateWorkStep("/step1");
            var step2 = _wr.CreateWorkStep("/step2");
            var step1Sub = _wr.CreateWorkStep("/step1/subStep", WorkStepType.Transient);

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(step1Sub, step2)
                );
        }

        [TestMethod]
        public void ShouldNotMoveDecendantOfTransientStep()
        {
            _wr.CreateWorkStep("/step1", WorkStepType.Transient);
            var step2 = _wr.CreateWorkStep("/step2");
            var step1Sub = _wr.CreateWorkStep("/step1/subStep");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _mover.MoveWorkStep(step1Sub, step2)
                );
        }


    }
}