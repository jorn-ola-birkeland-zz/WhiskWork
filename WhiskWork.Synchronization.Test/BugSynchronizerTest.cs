using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Synchronizer;

namespace WhiskWork.Synchronization.UnitTest
{
    [TestClass]
    public class BugSynchronizerTest
    {
        private MockRepository _mocks;
        private IWhiskWorkRepository _whiskWorkRepository;
        private IDominoRepository _dominoRepository;    

        [TestInitialize]
        public void Init()
        {
            _mocks = new MockRepository();
            _whiskWorkRepository = _mocks.Stub<IWhiskWorkRepository>();
            _dominoRepository = _mocks.Stub<IDominoRepository>();
        }

        [TestMethod]
        public void Test()
        {
            var synchronizer = new BugSynchronizer(_whiskWorkRepository,_dominoRepository);

            synchronizer.Synchronize();
        }
    }
}