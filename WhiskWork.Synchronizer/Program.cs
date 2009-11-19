#region

using System;
using WhiskWork.Core.Synchronization;

#endregion

namespace WhiskWork.Synchronizer
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            if(args.Length<4)
            {
                Console.WriteLine("Usage: WhiskWork.Synchronizer.exe <webhost[:port]> <dominohost[:port]> <dominologin> <dominopassword> [-release:<release>] [-team:<team1[,team2,...]>]");
                return;
            }

            var host = args[0];
            var eManagerSynchronizationAgent = CreateEManagerSynchronizationAgent(args);

            const string rootPath = "/";
            const string beginStep = "/scheduled";


            //var eManagerAgent = new CachingSynchronizationAgent(eManagerSynchronizationAgent);
            var eManagerAgent = eManagerSynchronizationAgent;

            var whiskWorkAgent = new WhiskWorkSynchronizationAgent(host, rootPath, beginStep);

            var statusMap = new SynchronizationMap(eManagerAgent, whiskWorkAgent);
            statusMap.AddReciprocalEntry("0a - Scheduled for development", "/scheduled");
            
            statusMap.AddReciprocalEntry("2 - Development", "/analysis/inprocess");
            statusMap.AddReverseEntry("/anlysis/done", "2 - Development");
            statusMap.AddReverseEntry("/development/inprocess", "2 - Development");
            statusMap.AddReciprocalEntry("3 - Ready for test", "/development/done");
            statusMap.AddReverseEntry("/feedback/review", "3 - Ready for test");
            statusMap.AddReverseEntry("/feedback/test", "3 - Ready for test");

            statusMap.AddReciprocalEntry("4a ACCEPTED - In Dev", "/done");
            statusMap.AddForwardEntry("4a FAILED - In Dev", "/development/inprocess");
            statusMap.AddForwardEntry("4b ACCEPTED - In Test", "/done");
            statusMap.AddForwardEntry("4b FAILED - In Test", "/done");
            statusMap.AddForwardEntry("4c ACCEPTED - In Stage", "/done");
            statusMap.AddForwardEntry("4c FAILED - In Stage", "/done");
            statusMap.AddForwardEntry("5 - Approved (ready for deploy)", "/done");
            statusMap.AddForwardEntry("7 - Deployed to prod", "/done");
            var creationSynchronizer = new CreationSynchronizer(statusMap, eManagerAgent, whiskWorkAgent);
            var statusSynchronizer = new StatusSynchronizer(statusMap, whiskWorkAgent, eManagerAgent);

            var propertyMap = new SynchronizationMap(eManagerAgent, whiskWorkAgent);
            propertyMap.AddReciprocalEntry("name", "name");
            propertyMap.AddReciprocalEntry("unid", "unid");
            propertyMap.AddReciprocalEntry("title","title");
            propertyMap.AddReciprocalEntry("team","team");
            propertyMap.AddReciprocalEntry("release","release");
            propertyMap.AddReciprocalEntry("project","project");
            propertyMap.AddReciprocalEntry("leanstatus","leanstatus");
            propertyMap.AddReciprocalEntry("priority","priority");
            var propertySynchronizer = new DataSynchronizer(propertyMap, eManagerAgent, whiskWorkAgent);
            propertySynchronizer.SynchronizeOrdinal = true;

            var responsibleMap = new SynchronizationMap(whiskWorkAgent,eManagerAgent);
            responsibleMap.AddReciprocalEntry("unid", "unid");
            responsibleMap.AddReciprocalEntry("responsible", "Person");
            var responsibleSynchronizer = new DataSynchronizer(responsibleMap, whiskWorkAgent, eManagerAgent);

            Console.WriteLine("Synchronizing existence (eManager->whiteboard)");
            creationSynchronizer.Synchronize();

            Console.WriteLine("Synchronizing status whiteboard->eManager");
            statusSynchronizer.Synchronize();

            Console.WriteLine("Synchronizing properties eManager->whiteboard");
            propertySynchronizer.Synchronize();

            Console.WriteLine("Synchronizing responsible whiteboard->eManager");
            responsibleSynchronizer.Synchronize();

        }

        private static EManagerSynchronizationAgent CreateEManagerSynchronizationAgent(string[] args)
        {
            var dominohost = args[1];
            var login = args[2];
            var password = args[3];

            var eManagerSynchronizationAgent = new EManagerSynchronizationAgent(dominohost, login, password);

            if(args.Length>3)
            {
                for(int i=3;i<args.Length;i++)
                {
                    var keyValue = args[i].Split(':');

                    switch(keyValue[0].ToLowerInvariant())
                    {
                        case "-release":
                            eManagerSynchronizationAgent.Release = keyValue[1];
                            break;
                        case "-team":
                            eManagerSynchronizationAgent.Team = keyValue[1];
                            break;
                    }
                }
            }
            return eManagerSynchronizationAgent;
        }
    }
}