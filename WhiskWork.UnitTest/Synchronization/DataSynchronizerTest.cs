using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Core.UnitTest.Synchronization
{
    [TestClass]
    public class DataSynchronizerTest : SynchronizerTestBase
    {
        private ISynchronizationAgent _masterStub;
        private ISynchronizationAgent _slaveMock;
        private SynchronizationMap _propertyMap;
        private DataSynchronizer _synchronizer;


        [TestInitialize]
        public void Init()
        {
            Inititialize();
            _masterStub = Mocks.Stub<ISynchronizationAgent>();
            _slaveMock = Mocks.StrictMock<ISynchronizationAgent>();
            _propertyMap = new SynchronizationMap(_masterStub, _slaveMock);
            _synchronizer = new DataSynchronizer(_propertyMap, _masterStub, _slaveMock);
        }

        [TestMethod]
        public void ShouldOnlyUpdateConfiguredProperties()
        {
            _propertyMap.AddReciprocalEntry("Dev", "Dev");


            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", "Name", "name1", "Dev", "dev1") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", "Name", "name2", "Dev", "dev2") });
                _slaveMock.UpdateData(Entry("1", "Development", "Dev", "dev1"));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }

        }

        [TestMethod]
        public void ShouldMapPropertyNames()
        {
            _propertyMap.AddReciprocalEntry("responsible", "Person");

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", "responsible", "value1") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", "Person", "value2") });
                _slaveMock.UpdateData(Entry("1", "Development", "Person", "value1"));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }

        }

        [TestMethod]
        public void ShouldAddMissingProperty()
        {
            _propertyMap.AddReciprocalEntry("Name", "Name");
            _propertyMap.AddReciprocalEntry("Dev", "Dev");


            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done","Name","name1","Dev","dev1") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development","Name","name2") });
                _slaveMock.UpdateData(Entry("1", "Development", "Name","name1","Dev","dev1"));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldRemoveExtraProperty()
        {
            _propertyMap.AddReciprocalEntry("Name", "Name");


            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done") });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", "Name", "name1") });
                _slaveMock.UpdateData(Entry("1", "Development", "Name", null));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }


        [TestMethod]
        public void ShouldUpdateDifferingOrdinal()
        {
            _synchronizer.SynchronizeOrdinal = true;

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", 4), Entry("2", "/done", 2) });

                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", 1), Entry("2", "Development", 2) });
                _slaveMock.UpdateData(Entry("1", "Development", 4));
                LastCall.Repeat.Once();

            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldNotUpdateDataWhenBothOrdinalsAreNull()
        {
            _synchronizer.SynchronizeOrdinal = true;

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", (int?)null) });
                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", (int?)null) });
            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }

        [TestMethod]
        public void ShouldNotUpdateDataWhenOrdinalsAreEqual()
        {
            _synchronizer.SynchronizeOrdinal = true;

            using (Mocks.Record())
            {
                SetupResult.For(_masterStub.GetAll()).Return(new[] { Entry("1", "/done", 1) });
                Expect.Call(_slaveMock.GetAll()).Return(new[] { Entry("1", "Development", 1) });
            }
            using (Mocks.Playback())
            {
                _synchronizer.Synchronize();
            }
        }



    }
}