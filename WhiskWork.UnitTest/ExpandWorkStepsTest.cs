using System;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using System.Linq;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class ExpandWorkStepsTest
    {
        private TestWorkflowRepository _workflowRepository;
        private TestWorkItemRepository _workItemRepository;
        private Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workflowRepository = new TestWorkflowRepository();
            _workItemRepository = new TestWorkItemRepository();
            _wp = new Workflow(_workflowRepository, _workItemRepository);

            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal,
                                    "task");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1,
                                    WorkStepType.Begin, "task");
            _workflowRepository.Add("/development/inprocess/tasks/inprocess", "/development/inprocess/tasks", 2,
                                    WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 3,
                                    WorkStepType.End, "task");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/done", "/", 1, WorkStepType.End, "cr");
        }

        [TestMethod]
        public void ShouldCreateWorkItemInBeginStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            Assert.AreEqual("/analysis", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemInNormalStep()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem("cr1", "/development"));
        }


        [TestMethod]
        public void ShouldNotCreateWorkItemInExpandStep()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem("cr1", "/development/inprocess"));
        }

        [TestMethod]
        public void ShouldMoveToTransientStepWhenEnteringSubRoot()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.AreEqual("/development/inprocess/cr1", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldCreateTransientWorkStepsWhenWorkItemEnterSubRoot()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.AreEqual(WorkStepType.Transient, _workflowRepository.GetWorkStep("/development/inprocess/cr1").Type);
            Assert.AreEqual(WorkStepType.Normal,
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks").Type);
            Assert.AreEqual(WorkStepType.Begin,
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks/new").Type);
            Assert.AreEqual(WorkStepType.Normal,
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks/inprocess").Type);
            Assert.AreEqual(WorkStepType.End,
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks/done").Type);
        }

        [TestMethod]
        public void ShouldCreateChildItemOfExpandedWorkItemInLeafStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development/inprocess", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            Assert.AreEqual("/development/inprocess/cr1/tasks/new", _workItemRepository.GetWorkItem("cr1.1").Path);
        }

        [TestMethod]
        public void ShouldCreateChildItemOfExpandedWorkItemAsProperChildItem()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development/inprocess", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            Assert.AreEqual("cr1", _workItemRepository.GetWorkItem("cr1.1").ParentId);
        }

        [TestMethod]
        public void ShouldLockExpandedWorkItemWhenChildItemIsCreated()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development/inprocess", new NameValueCollection());
            Assert.AreEqual(WorkItemStatus.Normal, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);
        }

        [TestMethod]
        public void ShouldMoveUnlockedExpandedWorkItemFromExpandStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development/inprocess", new NameValueCollection());
            Assert.AreEqual("/development/inprocess/cr1", _workItemRepository.GetWorkItem("cr1").Path);

            _wp.UpdateWorkItem("cr1", "/development/done", new NameValueCollection());
            Assert.AreEqual("/development/done", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldNotMoveLockedExpandedWorkItemFromExpandStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development/inprocess", new NameValueCollection());
            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            AssertUtils.AssertThrows<InvalidOperationException>(() =>
                                                                _wp.UpdateWorkItem("cr1", "/development/done",
                                                                                   new NameValueCollection())
                );
        }

        [TestMethod]
        public void ShouldDeleteChildStepsWhenMovingFromTransientExpandStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.IsTrue(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1"));
            Assert.IsTrue(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            Assert.IsTrue(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/new"));
            Assert.IsTrue(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/inprocess"));
            Assert.IsTrue(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/done"));

            _wp.UpdateWorkItem("cr1", "/development/done", new NameValueCollection());
            Assert.IsFalse(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1"));
            Assert.IsFalse(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            Assert.IsFalse(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/new"));
            Assert.IsFalse(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/inprocess"));
            Assert.IsFalse(_workflowRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/done"));
        }

        [TestMethod]
        public void ShouldSetWorkItemClassOnTransientSteps()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.AreEqual("cr-cr1", _workflowRepository.GetWorkStep("/development/inprocess/cr1").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks/new").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks/inprocess").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workflowRepository.GetWorkStep("/development/inprocess/cr1/tasks/done").WorkItemClass);
        }

        [TestMethod]
        public void ShouldNotMoveOtherWorkItemToTransientStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());
            Assert.AreEqual("/development/inprocess/cr1",_workItemRepository.GetWorkItem("cr1").Path);

            _wp.CreateWorkItem("cr2", "/analysis");
            AssertUtils.AssertThrows<InvalidOperationException>(
                ()=>_wp.UpdateWorkItem("cr2", "/development/inprocess/cr1", new NameValueCollection())
                );
        }


        [TestMethod]
        public void ShouldGiveChildOfExpandedItemCorrectClasses()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());
            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1/tasks/new");

            Assert.IsTrue(_workItemRepository.GetWorkItem("cr1.1").Classes.SetEquals("task","task-cr1"));
        }

        [TestMethod]
        public void ShouldNotMoveChildItemsCrossTransientSteps()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.CreateWorkItem("cr2", "/analysis");

            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());
            _wp.UpdateWorkItem("cr2", "/development", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            _wp.CreateWorkItem("cr2.1", "/development/inprocess/cr2/tasks/new");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () =>
                _wp.UpdateWorkItem("cr1.1",
                                   "/development/inprocess/cr2/tasks/inprocess",
                                   new NameValueCollection())
                );
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemsBelowExpandStep()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () =>
                _wp.CreateWorkItem("cr1.1", "/development/inprocess/tasks/new")
                );
        }
        
        [TestMethod]
        public void ShouldRemoveExpandLockWhenSingleChildItemIsDone()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem("cr1.1", "/development/inprocess/cr1/tasks/done", new NameValueCollection());
            Assert.AreEqual(WorkItemStatus.Normal, _workItemRepository.GetWorkItem("cr1").Status);
        }

        [TestMethod]
        public void ShouldNotRemoveExpandLockWhenOneOfTwoChildItemsIsDone()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            _wp.CreateWorkItem("cr1.2", "/development/inprocess/cr1");
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem("cr1.1", "/development/inprocess/cr1/tasks/done", new NameValueCollection());
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);
        }


        [TestMethod]
        public void ShouldSetExpandLockWhenASingleDoneChildIsMovedToNotEndStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem("cr1.1", "/development/inprocess/cr1/tasks/done", new NameValueCollection());
            Assert.AreEqual(WorkItemStatus.Normal, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem("cr1.1", "/development/inprocess/cr1/tasks/new", new NameValueCollection());
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);
        }

        [TestMethod]
        public void ShouldRemoveTransientStepClassWhenMovedFromTransientStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.IsTrue(_workItemRepository.GetWorkItem("cr1").Classes.SetEquals("cr","cr-cr1"));

            _wp.UpdateWorkItem("cr1", "/done", new NameValueCollection());
            Assert.IsTrue(_workItemRepository.GetWorkItem("cr1").Classes.SetEquals("cr"));

        }

        [TestMethod]
        public void ShouldNotBeAbleToMoveChildItemsWhenWorkItemIsMovedOutOfTransientStep()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            _wp.CreateWorkItem("cr1.1", "/development/inprocess/cr1");
            _wp.UpdateWorkItem("cr1.1", "/development/inprocess/cr1/tasks/done", new NameValueCollection());

            _wp.UpdateWorkItem("cr1", "/done", new NameValueCollection());

            AssertUtils.AssertThrows<ArgumentException>(
                ()=> _wp.UpdateWorkItem("cr1,1", "/development/inprocess/cr1/tasks/new", new NameValueCollection())
                );
        }

        [TestMethod, Ignore]
        public void ShouldBeAbleToMoveChildItemsWhenWorkItemIsMovedBackToExpandStep()
        {
        }

    }
}