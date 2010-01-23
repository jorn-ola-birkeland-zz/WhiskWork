using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Core.UnitTest.Synchronization
{
    [TestClass]
    public class ExistenceSynchronizerTest : SynchronizerTestBase
    {
        private ISynchronizationAgent _masterStub;
        private ISynchronizationAgent _slaveMock;
        private CreationSynchronizer _synchronizer;

        [TestInitialize]
        public void Init()
        {
            Inititialize();
            _masterStub = Mocks.Stub<ISynchronizationAgent>();
            _slaveMock = Mocks.StrictMock<ISynchronizationAgent>();
            var map = new SynchronizationMap(_masterStub, _slaveMock);

            map.AddReciprocalEntry("scheduled", "planned");
            _synchronizer = new CreationSynchronizer(map, _masterStub, _slaveMock);
        }

        [TestMethod]
        public void ShouldCreateMissingSynchronizationEntriesInSlave()
        {
            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "scheduled") });

                Expect.Call(_slaveMock.GetAll()).Return(new SynchronizationEntry[0]);
                _slaveMock.Create(Entry("1", "planned"));

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldIgnoreMissingMappingWhenSynchronizingCreation()
        {
            var map = new SynchronizationMap(_masterStub, _slaveMock);
            var synchronizer = new CreationSynchronizer(map, _masterStub, _slaveMock);

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "notMapped") });
                Expect.Call(_slaveMock.GetAll()).Return(new SynchronizationEntry[0]);
            }
            using (Mocks.Playback())
            {
                synchronizer.Synchronize();
            }

        }

        [TestMethod]
        public void ShouldDeleteSuperfluousSynchronizationEntiesInSlave()
        {
            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "scheduled") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "planned"), Entry("2", "planned") });
                _slaveMock.Delete(Entry("2", "planned"));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldDeleteEntriesMissingInMap()
        {
            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "scheduled"), Entry("2","deleted") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "planned"), Entry("2", "planned") });
                _slaveMock.Delete(Entry("2", "planned"));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
            
        }

        [TestMethod]
        public void ShouldIgnorePropertiesWhenCreating()
        {
            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "scheduled","key","value") });

                Expect.Call(_slaveMock.GetAll()).Return(new SynchronizationEntry[0] );
                _slaveMock.Create(Entry("1", "planned"));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

    }
}