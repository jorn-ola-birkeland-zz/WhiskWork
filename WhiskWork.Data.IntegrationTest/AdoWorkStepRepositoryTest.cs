using System.Linq;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using WhiskWork.Data.Ado;

namespace WhiskWork.Data.IntegrationTest
{
    [TestClass]
    public class AdoWorkStepRepositoryTest
    {
        private const string _connectionString = @"Data Source=BEKK-JORNOB;Initial Catalog=WhiskWorkTest;Integrated Security=SSPI;";
        private TransactionScope _tx;
        private AdoWorkStepRepository _repository;
        private WorkStep _ws;

        [TestInitialize]
        public void Init()
        {
            _tx = new TransactionScope();

            _repository = new AdoWorkStepRepository(_connectionString);
            _ws = WorkStep.New("/path");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _tx.Dispose();
        }

        [TestMethod]
        public void ShouldCreateAndReadPath()
        {
            _repository.CreateWorkStep(_ws);
            var actual =_repository.GetWorkStep(_ws.Path);

            Assert.AreEqual(_ws.Path,actual.Path);
        }

        [TestMethod]
        public void ShouldCreateAndReadOrdinal()
        {
            _repository.CreateWorkStep(_ws.UpdateOrdinal(3));
            var actual = _repository.GetWorkStep(_ws.Path);

            Assert.AreEqual(3, actual.Ordinal);
        }

        [TestMethod]
        public void ShouldCreateAndReadTitle()
        {
            _repository.CreateWorkStep(_ws.UpdateTitle("title"));
            var actual = _repository.GetWorkStep(_ws.Path);

            Assert.AreEqual("title", actual.Title);
        }

        [TestMethod]
        public void ShouldCreateAndReadType()
        {
            _repository.CreateWorkStep(_ws.UpdateType(WorkStepType.Expand));
            var actual = _repository.GetWorkStep(_ws.Path);

            Assert.AreEqual(WorkStepType.Expand, actual.Type);
        }

        [TestMethod]
        public void ShouldCreateAndReadWorkItemClass()
        {
            _repository.CreateWorkStep(_ws.UpdateWorkItemClass("class1"));
            var actual = _repository.GetWorkStep(_ws.Path);

            Assert.AreEqual("class1", actual.WorkItemClass);
        }

        [TestMethod]
        public void ShouldCreateAndReadWipLimit()
        {
            _repository.CreateWorkStep(_ws.UpdateWipLimit(2));
            var actual = _repository.GetWorkStep(_ws.Path);

            Assert.AreEqual(2, actual.WipLimit);
        }

        [TestMethod]
        public void ShouldDeleteWorkStep()
        {
            _repository.CreateWorkStep(_ws);
            Assert.IsTrue(_repository.ExistsWorkStep(_ws.Path));

            _repository.DeleteWorkStep(_ws.Path);
            Assert.IsFalse(_repository.ExistsWorkStep(_ws.Path));
        }

        [TestMethod]
        public void ShouldUpdateOrdinal()
        {
            _repository.CreateWorkStep(_ws.UpdateOrdinal(1));
            _repository.UpdateWorkStep(_ws.UpdateOrdinal(2));


            var actual = _repository.GetWorkStep(_ws.Path);
            Assert.AreEqual(2,actual.Ordinal);
        }

        [TestMethod]
        public void ShouldGetChildStepsOfRoot()
        {
            _repository.CreateWorkStep(WorkStep.New("/path1"));
            _repository.CreateWorkStep(WorkStep.New("/path2"));

            Assert.AreEqual(2,_repository.GetChildWorkSteps(WorkStep.Root.Path).Count());
        }

        [TestMethod]
        public void ShouldGetChildSteps()
        {
            _repository.CreateWorkStep(WorkStep.New("/path1/sub1"));
            _repository.CreateWorkStep(WorkStep.New("/path2/sub2"));
            _repository.CreateWorkStep(WorkStep.New("/path1/sub2"));

            Assert.AreEqual(2, _repository.GetChildWorkSteps("/path1").Count());
            Assert.AreEqual(1, _repository.GetChildWorkSteps("/path2").Count());
        }

        [TestMethod]
        public void ShouldGetAllWorkSteps()
        {
            _repository.CreateWorkStep(WorkStep.New("/path1/sub1"));
            _repository.CreateWorkStep(WorkStep.New("/path2/sub2"));
            _repository.CreateWorkStep(WorkStep.New("/path1/sub2"));

            Assert.AreEqual(3, _repository.GetAllWorkSteps().Count());
        }


    }
}
