using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Test.Common;
using System;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkStepCreatorTest
    {
        MemoryWorkStepRepository _workStepRepository;
        MemoryWorkItemRepository _workItemRepository;
        WorkStepCreator _creator;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _creator = new WorkStepCreator(new WorkflowRepository(_workItemRepository, _workStepRepository));
        }

        [TestMethod]
        public void ShouldNotCreateWorkStepIfParentDoesNotExist()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/analysis/inprocess").UpdateWorkItemClass("class1"))
                );

        }

        [TestMethod]
        public void ShouldCreateWorkStepWithSameWorkItemClassAsParentIfMissing()
        {
            _workStepRepository.CreateWorkStep(WorkStep.New("/analysis").UpdateWorkItemClass("class1"));

            Assert.AreEqual("class1", _workStepRepository.GetWorkStep("/analysis").WorkItemClass);
        }

        [TestMethod]
        public void ShouldCreateWorkStepWithSameWorkItemClassAsPrecedingSibilingIfMissingWorkItemClassAndNoParent()
        {
            _creator.CreateWorkStep(WorkStep.New("/analysis").UpdateWorkItemClass("class1"));
            _creator.CreateWorkStep(WorkStep.New("/development"));

            Assert.AreEqual("class1", _workStepRepository.GetWorkStep("/development").WorkItemClass);
        }

        [TestMethod]
        public void ShouldThrowExceptionIfCreatingWorkStepWithoutWorkItemClassWhenWorkItemClassCannotBeResolved()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                ()=>_creator.CreateWorkStep(WorkStep.New("/analysis"))
                );
        }

        [TestMethod]
        public void ShouldThrowIfCreatingWorkStepWithWorkItemClassDifferentFromParentWhenParentIsNormalStep()
        {
            _creator.CreateWorkStep(
                WorkStep.New("/analysis").UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/analysis/inprocess").UpdateWorkItemClass("task"))
                );
        }

        [TestMethod]
        public void ShouldThrowIfCreatingWorkStepWithWorkItemClassDifferentFromSiblings()
        {
            _creator.CreateWorkStep(
                WorkStep.New("/analysis").UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/development").UpdateWorkItemClass("task"))
                );
        }


        [TestMethod]
        public void ShouldThrowExceptionWhenCreatingChildOfExpandWithSameWorkItemClassAsParent()
        {
            _creator.CreateWorkStep(
                WorkStep.New("/analysis").UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/analysis/inprocess").UpdateWorkItemClass("cr"))
                );
        }

        [TestMethod]
        public void ShouldCreateChildOfParallelStepWithParentWorkItemClassConcatenatedWithParentsLeafStepIfWorkItemClassIsMissing()
        {
            _creator.CreateWorkStep(
                WorkStep.New("/feedback").UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _creator.CreateWorkStep(WorkStep.New("/feedback/review"));

            Assert.AreEqual("cr-review",_workStepRepository.GetWorkStep("/feedback/review").WorkItemClass);
        }

        [TestMethod]
        public void ShouldThrowIfCreatingChildOfParallelStepWithOtherThanParentWorkItemClassConcatenatedWithParentsLeafStep()
        {
            _creator.CreateWorkStep(
                WorkStep.New("/feedback").UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/feedback/review").UpdateWorkItemClass("cr"))
                );
        }
        
        [TestMethod]
        public void ShouldGiveOrdinalZeroToFirstWorkStepWhenWithoutOrdinal()
        {
            _creator.CreateWorkStep(WorkStep.New("/analysis").UpdateWorkItemClass("cr"));
            Assert.AreEqual(0, _workStepRepository.GetWorkStep("/analysis").Ordinal);
        }

        [TestMethod]
        public void ShouldAppendWhenCreatingWorkStepWithoutOrdinal()
        {
            _creator.CreateWorkStep(WorkStep.New("/analysis").UpdateWorkItemClass("cr"));
            _creator.CreateWorkStep(WorkStep.New("/analysis/inprocess"));
            _creator.CreateWorkStep(WorkStep.New("/analysis/done"));
            Assert.AreEqual(1, _workStepRepository.GetWorkStep("/analysis/done").Ordinal);
        }

        [TestMethod]
        public void ShouldThrowExceptionIfSiblingHasSameOrdinalWhenCreatingWorkStep()
        {
            _creator.CreateWorkStep(WorkStep.New("/analysis").UpdateWorkItemClass("cr").UpdateOrdinal(1));
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/development").UpdateOrdinal(1))
                );
        }

        [TestMethod]
        public void ShouldNotCreateWorkStepWithTransientWorkStepType()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _creator.CreateWorkStep(WorkStep.New("/development").UpdateWorkItemClass("cr").UpdateType(WorkStepType.Transient))
                );
        }



    }
}
