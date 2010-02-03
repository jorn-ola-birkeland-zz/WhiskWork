using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Core.UnitTest.Synchronization
{
    [TestClass]
    public class StatusSynchronizerTest : SynchronizerTestBase
    {
        private ISynchronizationAgent _masterStub;
        private ISynchronizationAgent _slaveMock;
        private StatusSynchronizer _synchronizer;

        [TestInitialize]
        public void Init()
        {
            Inititialize();

            _masterStub = Mocks.Stub<ISynchronizationAgent>();
            _slaveMock = Mocks.StrictMock<ISynchronizationAgent>();

            var map = new SynchronizationMap(_masterStub, _slaveMock);
            map.AddReciprocalEntry("/analysis", "Development");
            map.AddReciprocalEntry("/done", "Done");

            _synchronizer = new StatusSynchronizer(map, _masterStub, _slaveMock);
        }

        [TestMethod]
        public void ShouldSynchronizeStatus()
        {

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development") });
                _slaveMock.UpdateStatus(Entry("1", "Done"));

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldNotUpdateNewerStatus()
        {
            DateTime now = DateTime.Now;

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", now) });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development",now.AddSeconds(1)) });

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
           
        }

        [TestMethod]
        public void ShouldUpdateOlderStatus()
        {

            DateTime now = DateTime.Now;
            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", now) });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", now.AddSeconds(-1)) });
                _slaveMock.UpdateStatus(Entry("1", "Done", now));

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }


        [TestMethod]
        public void ShouldIgnoreMissingMappingWhenSynchronizingStatus()
        {
            var map = new SynchronizationMap(_masterStub, _slaveMock);
            _synchronizer = new StatusSynchronizer(map, _masterStub, _slaveMock);

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done") });
                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "UnknownMap") });
            }

            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldSynchronizeStatusIfSlaveEntriesAreMissing()
        {
            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done"), Entry("2", "/analysis") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("2", "Done") });
                _slaveMock.UpdateStatus(Entry("2", "Development"));

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }
    }
}