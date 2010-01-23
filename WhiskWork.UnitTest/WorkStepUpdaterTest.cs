#region

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Test.Common;

#endregion

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkStepUpdaterTest
    {
        private WorkStepUpdater _updater;
        private MemoryWorkItemRepository _workItemRepository;
        private MemoryWorkStepRepository _workStepRepository;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _updater = new WorkStepUpdater(new WorkflowRepository(_workItemRepository, _workStepRepository));
        }


        [TestMethod]
        public void ShouldUpdateWorkStepOrdinal()
        {
            AssertUpdate(2, 3, (ws, value) => ws.UpdateOrdinal(value), ws => ws.Ordinal.Value);
        }

        [TestMethod]
        public void ShouldUpdateType()
        {
            AssertUpdate(WorkStepType.Normal, WorkStepType.Expand, (ws, value) => ws.UpdateType(value), ws => ws.Type);
        }

        [TestMethod]
        public void ShouldUpdateWorkItemClass()
        {
            AssertUpdate("class1", "class2", (ws, value) => ws.UpdateWorkItemClass(value), ws => ws.WorkItemClass);
        }


        [TestMethod]
        public void ShouldUpdateTitle()
        {
            AssertUpdate("title1", "title2", (ws, value) => ws.UpdateTitle(value), ws => ws.Title);
        }

        [TestMethod]
        public void ShouldUpdateWipLimit()
        {
            AssertUpdate(1, 2, (ws, value) => ws.UpdateWipLimit(value), ws => ws.WipLimit.Value);
        }


        [TestMethod]
        public void ShouldNotDeleteOrdinalWhenUpdatingWorkStepWithoutOrdinal()
        {
            AssertUnchangedAfterVoidUpdate(2, (ws, value) => ws.UpdateOrdinal(value), ws => ws.Ordinal.Value);
        }

        [TestMethod]
        public void ShouldNotDeleteTitleWhenUpdatingWorkStepWithoutTitle()
        {
            AssertUnchangedAfterVoidUpdate("title", (ws, value) => ws.UpdateTitle(value), ws => ws.Title);
        }

        [TestMethod]
        public void ShouldNotDeleteTypeWhenUpdatingWorkStepWithoutType()
        {
            AssertUnchangedAfterVoidUpdate(WorkStepType.Expand, (ws, value) => ws.UpdateType(value), ws => ws.Type);
        }

        [TestMethod]
        public void ShouldNotDeleteWorkItemClassWhenUpdatingWorkStepWithoutWorkItemClass()
        {
            AssertUnchangedAfterVoidUpdate("class1", (ws, value) => ws.UpdateWorkItemClass(value),
                                           ws => ws.WorkItemClass);
        }

        [TestMethod]
        public void ShouldNotDeleteWipLimitWhenUpdatingWorkStepWithoutWipLimit()
        {
            AssertUnchangedAfterVoidUpdate(1, (ws, value) => ws.UpdateWipLimit(value), ws => ws.WipLimit.Value);
        }

        [TestMethod]
        public void ShouldUpdateEmptyLeafStepToExpandStep()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis"));
            _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Expand));

            Assert.AreEqual(WorkStepType.Expand, _workStepRepository.GetWorkStep("/analysis").Type);
        }

        [TestMethod]
        public void ShouldNotUpdateToExpandStepIfItContainsWorkSteps()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis"));
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis/inprocess"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Expand))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateToExpandStepIfItContainsWorkItems()
        {
            var step = WorkStep.New("/analysis");
            _workStepRepository.CreateWorkStep(step);
            _workItemRepository.CreateWorkItem(step, "cr1");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(step.UpdateType(WorkStepType.Expand))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateFromExpandStepIfItContainsWorkSteps()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Expand));
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis/inprocess"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Normal))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateFromExpandStepIfItContainsWorkItems()
        {
            var step = WorkStep.New("/analysis");
            _workStepRepository.CreateWorkStep(step.UpdateType(WorkStepType.Expand));
            _workItemRepository.CreateWorkItem(step, "cr1");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(step.UpdateType(WorkStepType.Normal))
                );
        }

        [TestMethod]
        public void ShouldUpdateEmptyLeafStepToParallelStep()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis"));
            _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Parallel));

            Assert.AreEqual(WorkStepType.Parallel, _workStepRepository.GetWorkStep("/analysis").Type);
        }

        [TestMethod]
        public void ShouldNotUpdateToParallelStepIfContainsWorkSteps()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis"));
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis/inprocess"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Parallel))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateToParallelStepIfItContainsWorkItems()
        {
            var step = WorkStep.New("/analysis");
            _workStepRepository.CreateWorkStep(step);
            _workItemRepository.CreateWorkItem(step, "cr1");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(step.UpdateType(WorkStepType.Parallel))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateFromParallelStepIfItContainsWorkSteps()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Parallel));
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis/inprocess"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Normal))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateFromParallelStepIfItContainsWorkItems()
        {
            var step = WorkStep.New("/analysis");
            _workStepRepository.CreateWorkStep(step.UpdateType(WorkStepType.Parallel));
            _workItemRepository.CreateWorkItem(step, "cr1");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(step.UpdateType(WorkStepType.Normal))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateToTransientStep()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Normal));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Transient))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateTransientStep()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Transient));
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateTitle("title"))
                );
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(2))
                );
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateType(WorkStepType.Normal))
                );
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateWipLimit(2))
                );
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateWorkItemClass("cr"))
                );
        }


        [TestMethod]
        public void ShouldThrowExceptionIfSiblingHasSameOrdinalWhenUpdatingWorkStepOrdinal()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1));
            _workStepRepository.CreateWorkStep(WorkStep.New("/dev").UpdateOrdinal(2));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/dev").UpdateOrdinal(1))
                );
        }

        [TestMethod]
        public void ShouldNotUpdateToWipLimitWhichIsViolatedAtOutset()
        {
            var step = WorkStep.New("/analysis");
            _workStepRepository.CreateWorkStep(step);
            _workItemRepository.CreateWorkItem(step, "cr1","cr2");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _updater.UpdateWorkStep(WorkStep.New("/analysis").UpdateWipLimit(1))
                );
        }

        private void AssertUpdate<T>(T value1, T value2, Update<T> updater, Converter<WorkStep, T> propertyGetter)
        {
            const string path = "/analysis";

            var ws1 = WorkStep.New(path);
            _workStepRepository.CreateWorkStep(updater(ws1, value1));
            Assert.AreEqual(value1, propertyGetter(_workStepRepository.GetWorkStep(path)));

            _updater.UpdateWorkStep(updater(ws1, value2));
            Assert.AreEqual(value2, propertyGetter(_workStepRepository.GetWorkStep(path)));
        }

        private void AssertUnchangedAfterVoidUpdate<T>(T value, Update<T> updater, Converter<WorkStep, T> propertyGetter)
        {
            const string path = "/analysis";

            var ws1 = WorkStep.New(path);
            _workStepRepository.CreateWorkStep(updater(ws1, value));
            Assert.AreEqual(value, propertyGetter(_workStepRepository.GetWorkStep(path)));

            _updater.UpdateWorkStep(ws1);
            Assert.AreEqual(value, propertyGetter(_workStepRepository.GetWorkStep(path)));
        }

        #region Nested type: Update

        private delegate WorkStep Update<T>(WorkStep ws, T value);

        #endregion
    }
}