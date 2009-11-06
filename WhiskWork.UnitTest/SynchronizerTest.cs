using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class SynchronizerTest
    {
        private MockRepository _mocks;

        private ISynchronizationAgent _masterStub;
        private ISynchronizationAgent _slaveMock;

        private SynchronizationMap _map;

        [TestInitialize]
        public void Init()
        {
            _mocks = new MockRepository();

            _masterStub = _mocks.Stub<ISynchronizationAgent>();
            _slaveMock = _mocks.DynamicMock<ISynchronizationAgent>();

            _map = new SynchronizationMap(_masterStub, _slaveMock);

        }

        [TestMethod]
        public void ShouldCreateMissingSynchronizationEntriesInSlave()
        {
            _map.AddReciprocalEntry("scheduled", "planned");

            var synchronizer = new CreationSynchronizer(_map, _masterStub, _slaveMock);

            using(_mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "scheduled")});
               
                Expect.Call(_slaveMock.GetAll()).Return(new SynchronizationEntry[0]);
                _slaveMock.Create(Entry("1", "planned"));

            }
            using(_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldDeleteSuperfluousSynchronizationEntiesInSlave()
        {
            _map.AddReciprocalEntry("scheduled", "planned");
            
            var synchronizer = new CreationSynchronizer(_map, _masterStub, _slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(
                    new[] { Entry("1", "scheduled") });

                Expect.Call(_slaveMock.GetAll()).Return(
                    new[] { Entry("1", "planned"),Entry("2", "planned") });
                _slaveMock.Delete(Entry("2", "planned"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldSynchronizeStatus()
        {
            _map.AddReciprocalEntry("/analysis", "Development");
            _map.AddReciprocalEntry("/done", "Done");

            var synchronizer = new StatusSynchronizer(_map, _masterStub, _slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development") });
                _slaveMock.UpdateStatus(Entry("1","Done"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldSynchronizeStatusIfSlaveEntriesAreMissing()
        {
            _map.AddReciprocalEntry("/analysis", "Development");
            _map.AddReciprocalEntry("/done", "Done");

            var synchronizer = new StatusSynchronizer(_map, _masterStub, _slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done"),Entry("2","/analysis") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("2", "Done") });
                _slaveMock.UpdateStatus(Entry("2", "Development"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod, Ignore]
        public void ShouldUpdateProperties()
        {
            _map.AddReciprocalEntry("/analysis", "Development");
            _map.AddReciprocalEntry("/done", "Done");

            var synchronizer = new PropertySynchronizer(_map, _masterStub, _slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done","Name","name1","Dev","dev1") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development","Name","name2") });
                _slaveMock.UpdateProperties(Entry("1", "Development", "Name","name1","Dev","dev1"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
            
        }



        private static SynchronizationEntry Entry(string id, string status)
        {
            return new SynchronizationEntry(id, status, new Dictionary<string, string>());
        }

        private static SynchronizationEntry Entry(string id, string status, params string[] propertyKeyValues)
        {
            var properties = new Dictionary<string, string>();

            for (int i = 0; i < propertyKeyValues.Length;i=i+2)
            {
                properties.Add(propertyKeyValues[i],propertyKeyValues[i+1]);
            }

            return new SynchronizationEntry(id, status, properties);
        }

    }
}