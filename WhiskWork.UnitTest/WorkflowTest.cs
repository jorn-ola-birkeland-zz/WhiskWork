using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Test.Common;
using Rhino.Mocks;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkflowTest
    {
        MemoryWorkStepRepository _workStepRepository;
        MemoryWorkItemRepository _workItemRepository;
        Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _wp = new Workflow(new WorkflowRepository(_workItemRepository, _workStepRepository));
        }

        [TestMethod]
        public void ShouldCreateWorkItemInBeginStep()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            Assert.AreEqual("/analysis", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemInNormalStep()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem(WorkItem.New("cr1", "/development")));
        }


        [TestMethod]
        public void ShouldCreateWorkItemWithSingleProperty()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "CR1" } }));
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(1, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
        }

        [TestMethod]
        public void ShouldCreateWorkItemWithTwoProperties()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "CR1" }, { "Developers", "A, B" } }));
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(2, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
            Assert.AreEqual("A, B", workItem.Properties["Developers"]);
        }

        [TestMethod]
        public void ShouldUpdateOneOfTwoProperties()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "CR1" }, { "Developer", "A" } }));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "Developer", "B" } }));
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(2, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
            Assert.AreEqual("B", workItem.Properties["Developer"]);
        }

        [TestMethod]
        public void ShouldRemovePropertyIfValueIsEmpty()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "Name", "CR1" }, { "Developer", "A" } }));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "Developer", "" } }));
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(1, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
        }


        [TestMethod]
        public void ShouldMoveAndUpdateOneOfTwoProperties()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "CR1" }, { "Developer", "A" } }));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development", new NameValueCollection { { "Developer", "B" } }));
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(2, workItem.Properties.Count);
            Assert.AreEqual("/development", workItem.Path);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
            Assert.AreEqual("B", workItem.Properties["Developer"]);
        }

        [TestMethod]
        public void ShouldUpdatePropertyOfWorkItemInExpandStep()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess"));

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/inprocess", new NameValueCollection { { "Developer", "B" } }));

        }

        [TestMethod]
        public void ShouldSetSequentialOrdinalWhenCreatingWorkItemWithoutOrdinal()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2", "/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr3", "/analysis"));

            Assert.AreEqual(1, _workItemRepository.GetWorkItem("cr1").Ordinal);
            Assert.AreEqual(2, _workItemRepository.GetWorkItem("cr2").Ordinal);
            Assert.AreEqual(3, _workItemRepository.GetWorkItem("cr3").Ordinal);
        }

        [TestMethod]
        public void ShouldCreateWorkItemWithOrdinalsProvided()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1", "/development").UpdateOrdinal(3));
            _wp.CreateWorkItem(WorkItem.New("cr2", "/development").UpdateOrdinal(2));
            _wp.CreateWorkItem(WorkItem.New("cr3", "/development").UpdateOrdinal(1));

            Assert.AreEqual(3, _workItemRepository.GetWorkItem("cr1").Ordinal);
            Assert.AreEqual(2, _workItemRepository.GetWorkItem("cr2").Ordinal);
            Assert.AreEqual(1, _workItemRepository.GetWorkItem("cr3").Ordinal);
        }

        [TestMethod]
        public void ShouldUpdateWorkItemOrdinalWhenNotMoving()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "CR1" }, { "Developer", "A" } }));
            _wp.UpdateWorkItem(WorkItem.New("cr1","/analysis").UpdateOrdinal(3));
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(3, workItem.Ordinal);
        }

        [TestMethod]
        public void ShouldPreserveOrdinalWhenMovingWorkItem()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis").UpdateOrdinal(3));
            _wp.CreateWorkItem(WorkItem.New("cr2", "/analysis").UpdateOrdinal(4));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));

            Assert.AreEqual(3, _wp.GetWorkItem("cr1").Ordinal);
            Assert.AreEqual(4, _wp.GetWorkItem("cr2").Ordinal);
        }

        [TestMethod] 
        public void ShouldSetTimestampWhenCreatingWorkItem()
        {
            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;

            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            }

            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(expectedTime, workItem.Timestamp);

        }

        [TestMethod]
        public void ShouldSetLastMovedWhenCreatingWorkItem()
        {
            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;

            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            }

            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(expectedTime, workItem.LastMoved);

        }


        [TestMethod]
        public void ShouldOverrideTimestampWhenCreatingWorkItem()
        {
            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;

            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis").UpdateTimestamp(expectedTime.AddDays(1)));

            }

            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(expectedTime, workItem.Timestamp);

        }


        [TestMethod]
        public void ShouldSetTimestampWhenUpdatingWorkItem()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.UpdateWorkItem(WorkItem.New("cr1", "/analysis"));
                var workItem = _wp.GetWorkItem("cr1");
                Assert.AreEqual(expectedTime, workItem.Timestamp);
            }
        }

        [TestMethod]
        public void ShouldSetLastMovedWhenMovingWorkItem()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
                var workItem = _wp.GetWorkItem("cr1");
                Assert.AreEqual(expectedTime, workItem.LastMoved);
            }
            
        }

        [TestMethod]
        public void ShouldNotUpdateLastMovedWhenUpdatingWorkItem()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var mocks = new MockRepository();
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            var createdTime = _wp.GetWorkItem("cr1").LastMoved;

            var mocktime = new DateTime(2009, 3, 4);
            _wp.MockTime(mocks, mocktime);

            using (mocks.Playback())
            {
                _wp.UpdateWorkItem(WorkItem.New("cr1", "/analysis").UpdateProperty("name","value"));
                var workItem = _wp.GetWorkItem("cr1");
                Assert.AreEqual(createdTime, workItem.LastMoved);
            }
        }


        [TestMethod]
        public void ShouldThrowExceptionWhenUpdatingIfTimeStampIsSetAndDiffersFromLastTimeStamp()
        {
            var mocks = new MockRepository();
            var createTime = DateTime.Now;

            _wp.MockTime(mocks, createTime);

            using (mocks.Playback())
            {
                _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            }

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem(WorkItem.New("cr1", "/analysis").UpdateTimestamp(createTime.AddMilliseconds(1)))
                );
        }

        [TestMethod]
        public void ShouldUpdateWorkItemIfTimeStampIsSetAndEqualToLastTimeStamp()
        {
            var mocks = new MockRepository();
            var createTime = DateTime.Now;

            _wp.MockTime(mocks, createTime);

            using (mocks.Playback())
            {
                _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            }

            var updateTime = createTime.AddMilliseconds(1);
            _wp.MockTime(mocks, updateTime);

            using (mocks.Playback())
            {
                _wp.UpdateWorkItem(WorkItem.New("cr1", "/analysis").UpdateTimestamp(createTime));
            }

            var workItem = _wp.GetWorkItem("cr1");
            Assert.AreEqual(updateTime,workItem.Timestamp);
        }


    }
}