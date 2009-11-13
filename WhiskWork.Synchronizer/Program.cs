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


            var eManagerAgent = new CachingSynchronizationAgent(eManagerSynchronizationAgent);

            var whiskWorkAgent = new WhiskWorkSynchronizationAgent(host, rootPath, beginStep);

            var map = new StatusSynchronizationMap(eManagerAgent, whiskWorkAgent);
            map.AddReciprocalEntry("0a - Scheduled for development", "/scheduled");
            
            map.AddReciprocalEntry("2 - Development", "/analysis/inprocess");
            map.AddReverseEntry("/anlysis/done", "2 - Development");
            map.AddReverseEntry("/development/inprocess", "2 - Development");
            map.AddReciprocalEntry("3 - Ready for test", "/development/done");
            map.AddReverseEntry("/feedback/review", "3 - Ready for test");
            map.AddReverseEntry("/feedback/test", "3 - Ready for test");

            map.AddReciprocalEntry("4a ACCEPTED - In Dev", "/done");
            map.AddForwardEntry("4a FAILED - In Dev", "/development/inprocess");
            map.AddForwardEntry("4b ACCEPTED - In Test", "/done");
            map.AddForwardEntry("4b FAILED - In Test", "/done");
            map.AddForwardEntry("4c ACCEPTED - In Stage", "/done");
            map.AddForwardEntry("4c FAILED - In Stage", "/done");
            map.AddForwardEntry("5 - Approved (ready for deploy)", "/done");
            map.AddForwardEntry("7 - Deployed to prod", "/done");

            var creationSynchronizer = new CreationSynchronizer(map, eManagerAgent, whiskWorkAgent);
            var statusSynchronizer = new StatusSynchronizer(map, whiskWorkAgent, eManagerAgent);
            var dataSynchronizer = new DataSynchronizer(eManagerAgent, whiskWorkAgent);

            Console.WriteLine("Synchronizing existence (eManager->whiteboard)");
            creationSynchronizer.Synchronize();

            Console.WriteLine("Synchronizing status whiteboard->eManager");
            statusSynchronizer.Synchronize();
            
            Console.WriteLine("Synchronizing data eManager->whiteboard");
            dataSynchronizer.Synchronize();

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