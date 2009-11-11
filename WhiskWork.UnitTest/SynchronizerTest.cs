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


        [TestInitialize]
        public void Init()
        {
            _mocks = new MockRepository();
        }

        [TestMethod]
        public void ShouldCreateMissingSynchronizationEntriesInSlave()
        {
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.DynamicMock<ISynchronizationAgent>();
            var map = new StatusSynchronizationMap(masterStub, slaveMock);

            map.AddReciprocalEntry("scheduled", "planned");
            var synchronizer = new CreationSynchronizer(map, masterStub, slaveMock);

            using(_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(new[] { Entry("1", "scheduled")});
               
                Expect.Call(slaveMock.GetAll()).Return(new SynchronizationEntry[0]);
                slaveMock.Create(Entry("1", "planned"));

            }
            using(_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldIgnoreMissingMappingWhenSynchronizingCreation()
        {
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.StrictMock<ISynchronizationAgent>();
            var map = new StatusSynchronizationMap(masterStub, slaveMock);

            var synchronizer = new CreationSynchronizer(map, masterStub, slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(new[] { Entry("1", "notMapped") });
                Expect.Call(slaveMock.GetAll()).Return(new SynchronizationEntry[0]);
            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
            
        }

        [TestMethod]
        public void ShouldDeleteSuperfluousSynchronizationEntiesInSlave()
        {
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.DynamicMock<ISynchronizationAgent>();
            var map = new StatusSynchronizationMap(masterStub, slaveMock);

            map.AddReciprocalEntry("scheduled", "planned");
            var synchronizer = new CreationSynchronizer(map, masterStub, slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(
                    new[] { Entry("1", "scheduled") });

                Expect.Call(slaveMock.GetAll()).Return(
                    new[] { Entry("1", "planned"),Entry("2", "planned") });
                slaveMock.Delete(Entry("2", "planned"));
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
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.DynamicMock<ISynchronizationAgent>();

            var map = new StatusSynchronizationMap(masterStub, slaveMock);
            map.AddReciprocalEntry("/analysis", "Development");
            map.AddReciprocalEntry("/done", "Done");

            var synchronizer = new StatusSynchronizer(map, masterStub, slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(new[] { Entry("1", "/done") });

                Expect.Call(slaveMock.GetAll()).Return(new[] { Entry("1", "Development") });
                slaveMock.UpdateStatus(Entry("1","Done"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldIgnoreMissingMappingWhenSynchronizingStatus()
        {
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.StrictMock<ISynchronizationAgent>();
            var map = new StatusSynchronizationMap(masterStub, slaveMock);

            var synchronizer = new StatusSynchronizer(map, masterStub, slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(new[] { Entry("1", "/done") });
                Expect.Call(slaveMock.GetAll()).Return(new[] { Entry("1", "UnknownMap") });
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
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.DynamicMock<ISynchronizationAgent>();

            var map = new StatusSynchronizationMap(masterStub, slaveMock);
            map.AddReciprocalEntry("/analysis", "Development");
            map.AddReciprocalEntry("/done", "Done");

            var synchronizer = new StatusSynchronizer(map, masterStub, slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(new[] { Entry("1", "/done"),Entry("2","/analysis") });

                Expect.Call(slaveMock.GetAll()).Return(new[] { Entry("2", "Done") });
                slaveMock.UpdateStatus(Entry("2", "Development"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldUpdateProperties()
        {
            var masterStub = _mocks.Stub<ISynchronizationAgent>();
            var slaveMock = _mocks.DynamicMock<ISynchronizationAgent>();

            var synchronizer = new DataSynchronizer(masterStub, slaveMock);

            using (_mocks.Record())
            {
                SetupResult.For(masterStub.GetAll()).Return(new[] { Entry("1", "/done","Name","name1","Dev","dev1") });

                Expect.Call(slaveMock.GetAll()).Return(new[] { Entry("1", "Development","Name","name2") });
                slaveMock.UpdateData(Entry("1", "Development", "Name","name1","Dev","dev1"));
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