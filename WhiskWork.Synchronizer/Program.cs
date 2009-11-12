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

            if(args.Length!=1)
            {
                Console.WriteLine("Usage: WhiskWork.Synchronizer.exe <host[:port]> ");
                return;
            }

            var host = args[0];

            const string rootPath = "/";
            const string beginStep = "/scheduled";



            var eManagerAgent = new CachingSynchronizationAgent(new EManagerSynchronizationAgent("20091212", "CMS"));

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
    }
}