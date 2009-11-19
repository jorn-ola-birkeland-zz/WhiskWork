using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Generic;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class ExpandWorkStepsTest
    {
        private MemoryWorkStepRepository _workStepRepository;
        private MemoryWorkItemRepository _workItemRepository;
        private Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _wp = new Workflow(_workStepRepository, _workItemRepository);

            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workStepRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal,
                                    "task", "Tasks");
            _workStepRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1,
                                    WorkStepType.Begin, "task");
            _workStepRepository.Add("/development/inprocess/tasks/inprocess", "/development/inprocess/tasks", 2,
                                    WorkStepType.Normal, "task");
            _workStepRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 3,
                                    WorkStepType.End, "task");
            _workStepRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/done", "/", 1, WorkStepType.End, "cr");
        }


        [TestMethod]
        public void ShouldNotCreateWorkItemInExpandStep()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.Create("/development/inprocess","cr1"));
        }

        [TestMethod]
        public void ShouldMoveToExpandStep()
        {
            _wp.Create("/analysis","cr1");
            _wp.Move("/development","cr1");

            Assert.AreEqual("/development/inprocess", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldCreateTransientWorkStepsWhenWorkItemEnterExpandStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            Assert.AreEqual(WorkStepType.Transient, _workStepRepository.GetWorkStep("/development/inprocess/cr1").Type);
            Assert.AreEqual(WorkStepType.Normal,
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks").Type);
            Assert.AreEqual(WorkStepType.Begin,
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks/new").Type);
            Assert.AreEqual(WorkStepType.Normal,
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks/inprocess").Type);
            Assert.AreEqual(WorkStepType.End,
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks/done").Type);
        }

        [TestMethod]
        public void ShouldCreateTitleOnTransientChildSteps()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            Assert.AreEqual("Tasks", _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks").Title);
        }


        [TestMethod]
        public void ShouldNotIncludeTransientStepsOfEarlierWorkItemsWhenCreatingTransientSteps()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.CreateWorkItem(WorkItem.New("cr2","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr2", "/development"));

            AssertUtils.AssertThrows<ArgumentException>(
                () => _workStepRepository.GetWorkStep("/development/inprocess/cr2/cr1")
                );
        }


        [TestMethod]
        public void ShouldCreateChildItemOfExpandedWorkItemInLeafStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.AreEqual("/development/inprocess/cr1/tasks/new", _workItemRepository.GetWorkItem("cr1-1").Path);
        }

        [TestMethod]
        public void ShouldCreateChildItemOfExpandedWorkItemAsProperChildItem()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.AreEqual("cr1", _workItemRepository.GetWorkItem("cr1-1").Parent.Id);
        }

        [TestMethod]
        public void ShouldBeAbleToDeleteExpandedWorkItemWithoutChildren()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));

            Assert.IsTrue(_workItemRepository.ExistsWorkItem("cr1"));
            _wp.DeleteWorkItem("cr1");
            Assert.IsFalse(_workItemRepository.ExistsWorkItem("cr1"));
        }

        [TestMethod]
        public void ShouldLockExpandedWorkItemWhenChildItemIsCreated()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));
            Assert.AreEqual(WorkItemStatus.Normal, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);
        }

        [TestMethod]
        public void ShouldBeAbleToDeleteChildOfExpandedWorkItem()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.IsTrue(_workItemRepository.ExistsWorkItem("cr1-1"));
            _wp.DeleteWorkItem("cr1-1");
            Assert.IsFalse(_workItemRepository.ExistsWorkItem("cr1-1"));
        }

        [TestMethod]
        public void ShouldMoveUnlockedExpandedWorkItemFromExpandStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));
            Assert.AreEqual("/development/inprocess", _workItemRepository.GetWorkItem("cr1").Path);

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/done"));
            Assert.AreEqual("/development/done", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldNotMoveLockedExpandedWorkItemFromExpandStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/done")));
        }

        [TestMethod]
        public void ShouldAlsoDeleteChildrenWhenDeletingExpandedWorkItem()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));

            Assert.IsTrue(_wp.ExistsWorkItem("cr1"));
            Assert.IsTrue(_wp.ExistsWorkItem("cr1-1"));

            _wp.DeleteWorkItem("cr1");

            Assert.IsFalse(_wp.ExistsWorkItem("cr1"));
            Assert.IsFalse(_wp.ExistsWorkItem("cr1-1"));
        }

        [TestMethod]
        public void ShouldDeleteChildStepsWhenMovingFromExpandStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            Assert.IsTrue(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1"));
            Assert.IsTrue(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            Assert.IsTrue(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/new"));
            Assert.IsTrue(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/inprocess"));
            Assert.IsTrue(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/done"));

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/done"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/new"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/inprocess"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/done"));
        }

        [TestMethod]
        public void ShouldDeleteTransientStepsWhenDeletingExpandedWorkItem()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            _wp.DeleteWorkItem("cr1");
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/new"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/inprocess"));
            Assert.IsFalse(_workStepRepository.ExistsWorkStep("/development/inprocess/cr1/tasks/done"));
        }

        [TestMethod]
        public void ShouldSetWorkItemClassOnTransientSteps()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            Assert.AreEqual("cr-cr1", _workStepRepository.GetWorkStep("/development/inprocess/cr1").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks/new").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks/inprocess").WorkItemClass);
            Assert.AreEqual("task-cr1",
                            _workStepRepository.GetWorkStep("/development/inprocess/cr1/tasks/done").WorkItemClass);
        }


        [TestMethod]
        public void ShouldGiveChildOfExpandedItemCorrectClasses()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1/tasks/new"));

            Assert.IsTrue(_workItemRepository.GetWorkItem("cr1-1").Classes.SetEquals("task", "task-cr1"));
        }

        [TestMethod]
        public void ShouldNotMoveChildItemsCrossTransientSteps()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2","/analysis"));

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr2", "/development"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            _wp.CreateWorkItem(WorkItem.New("cr2-1","/development/inprocess/cr2/tasks/new"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/cr2/tasks/inprocess")));
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemsBelowExpandStep()
        {
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/tasks/new")));
        }

        [TestMethod]
        public void ShouldNotMoveChildOfWorkItemInExapndStepDirectlyToExpandStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess")));
        }

        [TestMethod]
        public void ShouldNotMoveChildOfWorkItemInExpandStepToStepUnderExpandStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/tasks/new")));
        }


        [TestMethod]
        public void ShouldRemoveExpandLockWhenSingleChildItemIsDone()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/cr1/tasks/done"));
            Assert.AreEqual(WorkItemStatus.Normal, _workItemRepository.GetWorkItem("cr1").Status);
        }

        [TestMethod]
        public void ShouldNotRemoveExpandLockWhenOneOfTwoChildItemsIsDone()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            _wp.CreateWorkItem(WorkItem.New("cr1-2","/development/inprocess/cr1"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/cr1/tasks/done"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);
        }


        [TestMethod]
        public void ShouldSetExpandLockWhenASingleDoneChildIsMovedToNotEndStep()
        {
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/cr1/tasks/done"));
            Assert.AreEqual(WorkItemStatus.Normal, _workItemRepository.GetWorkItem("cr1").Status);

            _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/cr1/tasks/new"));
            Assert.AreEqual(WorkItemStatus.ExpandLocked, _workItemRepository.GetWorkItem("cr1").Status);
        }


        [TestMethod]
        public void ShouldNotDeleteChildItemsWhenWorkItemIsMovedOutOfTransientStep()
        {
            _wp.Create("/analysis","cr1");
            _wp.Move("/development","cr1");

            _wp.Create("/development/inprocess/cr1","cr1-1");
            _wp.Move("/development/inprocess/cr1/tasks/done", "cr1-1");

            _wp.Move("/done","cr1");

            Assert.IsTrue(_wp.ExistsWorkItem("cr1-1"));
        }

    }
}