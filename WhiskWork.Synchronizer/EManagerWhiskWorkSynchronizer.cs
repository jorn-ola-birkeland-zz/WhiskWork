using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Net;
using WhiskWork.Core;
using WhiskWork.Core.Synchronization;
using WhiskWork.Web;

namespace WhiskWork.Synchronizer
{
    public abstract class EManagerWhiskWorkSynchronizer 
    {
        private bool _isSafeSynch;
        private bool _isDryRun;
        private ISynchronizationAgent _eManagerAgent;
        private ISynchronizationAgent _whiskWorkAgent;

        private readonly IWhiskWorkRepository _whiskWorkRepository;
        private readonly IDominoRepository _dominoRepository;

        public EManagerWhiskWorkSynchronizer(IWhiskWorkRepository whiskWorkRepository, IDominoRepository dominoRepository)
        {
            _whiskWorkRepository = whiskWorkRepository;
            _dominoRepository = dominoRepository;
        }

        public bool IsDryRun
        {
            get { return _isDryRun; }
            set { _isDryRun = value; }
        }

        public bool IsSafeSynch
        {
            get { return _isSafeSynch; }
            set { _isSafeSynch = value; }
        }

        protected abstract string WhiskWorkBeginStep { get; }
        protected abstract bool SynchronizeResponsibleEnabled { get; }
        protected abstract bool SynchronizeStatusReverseEnabled { get; }

        protected ISynchronizationAgent EManagerAgent
        {
            get { return _eManagerAgent; }
        }

        protected ISynchronizationAgent WhiskWorkAgent
        {
            get { return _whiskWorkAgent; }
        }


        public void Synchronize()
        {
            _eManagerAgent = CreateEManagerChangeRequestSynchronizationAgent();
            _whiskWorkAgent = CreateWhiskWorkSynchronizationAgent();

            var statusMap = CreateStatusMap();

            SynchronizeExistence(statusMap);
            SynchronizeProperties();
            SynchronizeStatus(statusMap);

            if(SynchronizeStatusReverseEnabled)
            {
                SynchronizeStatusReverse(statusMap);
            }

            if (SynchronizeResponsibleEnabled)
            {
                SynchronizeResponsible();
            }
        }




        private EManagerSynchronizationAgent CreateEManagerChangeRequestSynchronizationAgent()
        {
            var dominoSource = _dominoRepository;
            if (_isDryRun || _isSafeSynch)
            {
                dominoSource = new ReadOnlyDominoRepository(dominoSource);
            }

            var eManagerCrSynchronizationAgent = new EManagerSynchronizationAgent(dominoSource, MapFromEManager);


            return eManagerCrSynchronizationAgent;
        }


        private WhiskWorkSynchronizationAgent CreateWhiskWorkSynchronizationAgent()
        {
            IWhiskWorkRepository whiskWorkRepository = _whiskWorkRepository;

            if (_isDryRun)
            {
                whiskWorkRepository = new ReadOnlyWhiskWorkRepository(whiskWorkRepository);
            }

            return new WhiskWorkSynchronizationAgent(whiskWorkRepository, MapFromWhiskWork, WhiskWorkBeginStep);
        }


        private void SynchronizeStatus(SynchronizationMap statusMap)
        {
            if (!_isSafeSynch)
            {
                var statusSynchronizer = new StatusSynchronizer(statusMap, _whiskWorkAgent, _eManagerAgent);
                Console.WriteLine("Synchronizing status whiteboard->eManager");
                statusSynchronizer.Synchronize();
            }
            else
            {
                Console.WriteLine("Synchronizing status whiteboard->eManager DISABLED!");
            }
        }

        private void SynchronizeStatusReverse(SynchronizationMap statusMap)
        {
            var statusSynchronizer = new StatusSynchronizer(statusMap, _eManagerAgent, _whiskWorkAgent);
            Console.WriteLine("Synchronizing status eManager->whiteboard");
            statusSynchronizer.Synchronize();
        }


        private void SynchronizeExistence(SynchronizationMap statusMap)
        {
            var creationSynchronizer = new CreationSynchronizer(statusMap, _eManagerAgent, _whiskWorkAgent);
            Console.WriteLine("Synchronizing existence (eManager->whiteboard)");

            try
            {
                creationSynchronizer.Synchronize();
            }
            catch (WebException e)
            {
                Console.WriteLine(WebCommunication.ReadResponseToEnd(e.Response));
                throw;
            }
        }

        private void SynchronizeProperties()
        {
            DataSynchronizer propertySynchronizer = CreatePropertyMap();



            Console.WriteLine("Synchronizing properties eManager->whiteboard");
            try
            {
                propertySynchronizer.Synchronize();
            }
            catch (WebException e)
            {
                Console.WriteLine(WebCommunication.ReadResponseToEnd(e.Response));
                throw;
            }

        }

        private void SynchronizeResponsible()
        {
            var responsibleMap = new SynchronizationMap(_whiskWorkAgent, _eManagerAgent);
            responsibleMap.AddReciprocalEntry("unid", "unid");
            responsibleMap.AddReciprocalEntry("responsible", "CurrentPerson");
            var responsibleSynchronizer = new DataSynchronizer(responsibleMap, _whiskWorkAgent, _eManagerAgent);
            
            if (!_isSafeSynch)
            {
                Console.WriteLine("Synchronizing responsible whiteboard->eManager");
                responsibleSynchronizer.Synchronize();
            }
            else
            {
                Console.WriteLine("Synchronizing responsible whiteboard->eManager DISABLED!");
            }
        }

        protected abstract SynchronizationMap CreateStatusMap();

        protected abstract DataSynchronizer CreatePropertyMap();

        protected abstract IEnumerable<SynchronizationEntry> MapFromWhiskWork(IEnumerable<WorkItem> workItems);

        protected abstract SynchronizationEntry MapFromEManager(DataRow dataRow);

        protected static DateTime? ParseDominoTimeStamp(string timeStampText)
        {
            DateTime? timeStamp=null;
            DateTime tempTimeStamp;
            if(DateTime.TryParse(timeStampText,CultureInfo.CreateSpecificCulture("no"),DateTimeStyles.None,out tempTimeStamp))
            {
                timeStamp = tempTimeStamp;
            }
            return timeStamp;
        }
    }
}