using System;
using System.Linq;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using WhiskWork.Data.Ado;

namespace WhiskWork.Data.IntegrationTest
{
    [TestClass]
    public class AdoWorkItemRepositoryTest
    {
        private const string _connectionString = @"Data Source=BEKK-JORNOB;Initial Catalog=WhiskWorkTest;Integrated Security=SSPI;";
        private TransactionScope _tx;
        private AdoWorkItemRepository _repository;
        private WorkItem _wi;

        [TestInitialize]
        public void Init()
        {
            _tx = new TransactionScope(TransactionScopeOption.Required);

            _repository = new AdoWorkItemRepository(_connectionString);

            _wi = WorkItem.New("1", "/path/subpath");
        }

        [TestCleanup]
        public void CleanUp()
        {
            _tx.Dispose();
        }

        [TestMethod]    
        public void ShouldWriteAndReadWorkItemIdAndPath()
        {
            _repository.CreateWorkItem(_wi);
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(_wi.Id,actual.Id);
            Assert.AreEqual(_wi.Path, actual.Path);
        }

        [TestMethod]
        public void ShouldCreateAndReadOrdinal()
        {
            _repository.CreateWorkItem(_wi.UpdateOrdinal(3));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(3, actual.Ordinal);
        }

        [TestMethod]
        public void ShouldCreateAndReadNullOrdinal()
        {
            _repository.CreateWorkItem(_wi);
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.IsNull(actual.Ordinal);
        }
        
        [TestMethod]
        public void ShouldCreateAndReadLastMovedTime()
        {
            var expected = DateTime.Now;
            _repository.CreateWorkItem(_wi.UpdateLastMoved(expected));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(expected.ToString(), actual.LastMoved.Value.ToString());
        }

        [TestMethod]
        public void ShouldCreateAndReadTimestamp()
        {
            var expected = DateTime.Now;
            _repository.CreateWorkItem(_wi.UpdateTimestamp(expected));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(expected.ToString(), actual.Timestamp.Value.ToString());
        }

        [TestMethod]
        public void ShouldCreateAndReadStatus()
        {
            const WorkItemStatus expected = WorkItemStatus.ParallelLocked;
            _repository.CreateWorkItem(_wi.UpdateStatus(expected));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(expected, actual.Status);
        }

        [TestMethod]
        public void ShouldCreateAndReadParent()
        {
            _repository.CreateWorkItem(_wi.UpdateParent("parent",WorkItemParentType.Parallelled));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual("parent", actual.Parent.Id);
            Assert.AreEqual(WorkItemParentType.Parallelled, actual.Parent.Type);
        }

        [TestMethod]
        public void ShouldCreateAndReadClasses()
        {
            _repository.CreateWorkItem(_wi.UpdateClasses(new[] {"class1","class2"}));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual("class1", actual.Classes.ElementAt(0));
            Assert.AreEqual("class2", actual.Classes.ElementAt(1));
            
        }
        
        [TestMethod]
        public void ShouldCreateAndReadOneProperty()
        {
            _repository.CreateWorkItem(_wi.UpdateProperty("p1","v1"));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual("v1", actual.Properties["p1"]);
        }

        [TestMethod]
        public void ShouldCreateAndReadTwoProperties()
        {
            var wi = _wi.UpdateProperty("p1", "v1");
            wi = wi.UpdateProperty("p2", "v2");

            _repository.CreateWorkItem(wi);
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(2, actual.Properties.Count);
            Assert.AreEqual("v1", actual.Properties["p1"]);
            Assert.AreEqual("v2", actual.Properties["p2"]);
        }

        [TestMethod]
        public void ShouldCreateAndReadPropertyWithAmpersandAndEqualsInValue()
        {
            _repository.CreateWorkItem(_wi.UpdateProperty("p1","val&u=e"));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual("val&u=e", actual.Properties["p1"]);
        }

        [TestMethod]
        public void ShouldCreateAndReadPropertyWithAmpersandAndEqualsInKey()
        {
            _repository.CreateWorkItem(_wi.UpdateProperty("p&1=", "value"));
            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual("value", actual.Properties["p&1="]);
        }


        [TestMethod]
        public void ShouldUpdatePath()
        {
            var wi = _wi.MoveTo(WorkStep.New("/path2"),DateTime.Now);
            _repository.UpdateWorkItem(wi);

            var actual = _repository.GetWorkItem(wi.Id);

            Assert.AreEqual("/path2",actual.Path);

        }

        [TestMethod]
        public void ShouldDeleteWorkItem()
        {
            _repository.CreateWorkItem(_wi);
            Assert.IsTrue(_repository.ExistsWorkItem(_wi.Id));

            _repository.DeleteWorkItem(_wi.Id);
            Assert.IsFalse(_repository.ExistsWorkItem(_wi.Id));
        }

        [TestMethod]
        public void ShouldDeleteAndRecreateWorkItemWithProperties()
        {
            _repository.CreateWorkItem(_wi.UpdateProperty("p1","v1"));
            _repository.DeleteWorkItem(_wi.Id);
            _repository.CreateWorkItem(_wi.UpdateProperty("p2", "v2"));

            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(1, actual.Properties.Count);
            Assert.AreEqual("v2", actual.Properties["p2"]);
        }


        [TestMethod]
        public void ShouldRemovePropertiesWhenUpdatingWorkItemWithoutProperties()
        {
            _repository.CreateWorkItem(_wi.UpdateProperty("p1","v1"));
            _repository.UpdateWorkItem(_wi);

            var actual = _repository.GetWorkItem(_wi.Id);

            Assert.AreEqual(0,actual.Properties.Count);
        }

        [TestMethod]
        public void ShouldGetAllWorkItemsInAWorkStep()
        {
            _repository.CreateWorkItem(WorkItem.New("1","/path1"));
            _repository.CreateWorkItem(WorkItem.New("2", "/path1"));
            _repository.CreateWorkItem(WorkItem.New("3", "/path2"));

            Assert.AreEqual(2, _repository.GetWorkItems("/path1").Count());
        }

        [TestMethod]
        public void ShouldGetChildItems()
        {
            _repository.CreateWorkItem(WorkItem.New("1", "/path1"));
            _repository.CreateWorkItem(WorkItem.New("2", "/path1").UpdateParent("1",WorkItemParentType.Expanded));
            _repository.CreateWorkItem(WorkItem.New("3", "/path2").UpdateParent("1",WorkItemParentType.Parallelled));

            var children = _repository.GetChildWorkItems(new WorkItemParent("1", WorkItemParentType.Expanded));
            Assert.AreEqual(1, children.Count());
            Assert.AreEqual("2", children.Single().Id);
        }

        [TestMethod]
        public void ShouldGetAllWorkItemsItems()
        {
            _repository.CreateWorkItem(WorkItem.New("1", "/path1"));
            _repository.CreateWorkItem(WorkItem.New("2", "/path1").UpdateParent("1", WorkItemParentType.Expanded));
            _repository.CreateWorkItem(WorkItem.New("3", "/path2").UpdateParent("1", WorkItemParentType.Parallelled));
            _repository.CreateWorkItem(WorkItem.New("4", "/path2/path3").UpdateProperty("p1","v1"));

            var all = _repository.GetAllWorkItems();
            Assert.AreEqual(4, all.Count());
            Assert.AreEqual("v1", all.Where(wi=>wi.Id=="4").Single().Properties["p1"]);
        }

    }
}
