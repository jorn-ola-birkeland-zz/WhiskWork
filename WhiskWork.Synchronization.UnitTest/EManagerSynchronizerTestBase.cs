using System;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using Rhino.Mocks;
using WhiskWork.Synchronizer;

namespace WhiskWork.Synchronization.UnitTest
{
    public abstract class EManagerSynchronizerTestBase
    {
        private MockRepository Mocks { get; set; }
        protected IWhiskWorkRepository WhiskWorkRepository { get; private set; }
        protected IDominoRepository DominoRepository { get; private set; }
        private EManagerWhiskWorkSynchronizer Synchronizer { get; set; }

        protected void StubDominoAndMockWhiskWork()
        {
            Mocks = new MockRepository();
            WhiskWorkRepository = Mocks.StrictMock<IWhiskWorkRepository>();
            DominoRepository = Mocks.Stub<IDominoRepository>();

            CreateSynchronizer();
        }

        protected void StubWhiskWorkAndMockDomino()
        {
            Mocks = new MockRepository();
            WhiskWorkRepository = Mocks.Stub<IWhiskWorkRepository>();
            DominoRepository = Mocks.StrictMock<IDominoRepository>();

            CreateSynchronizer();
        }


        protected void CreateSynchronizer()
        {
            Synchronizer = CreateSynchronizer(WhiskWorkRepository, DominoRepository);
        }

        protected abstract EManagerWhiskWorkSynchronizer CreateSynchronizer(IWhiskWorkRepository whiskWorkRepository,
                                                                            IDominoRepository dominoRepository);

        protected void Record(Action action)
        {
            using(Mocks.Record())
            {
                action();
            }
        }

        protected void Playback()
        {
            using (Mocks.Playback())
            {
                Synchronizer.Synchronize();
            }
        }

        protected static string DominoFormatDateTime(DateTime timeStamp)
        {
            return timeStamp.ToString("dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
        }
    }
}