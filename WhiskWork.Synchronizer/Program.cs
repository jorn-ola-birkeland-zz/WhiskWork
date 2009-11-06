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
            const string rootPath = "/";
            const string site = "http://localhost:5555";
            const string beginStep = "/scheduled";

            //var eManagerAgent = new EManagerSynchronizationAgent("20091212", "CMS");

            var whiskWorkAgent = new WhiskWorkSynchronizationAgent(site, rootPath, beginStep);

            //var map = new SynchronizationMap(eManagerAgent, whiskWorkAgent);
            //map.AddReciprocalEntry("3. Scheduled for development", "/scheduled");
            //map.AddReciprocalEntry("2. Development", "/wip/analysis/inprocess");
            //map.AddReverseEntry("/wip/anlysis/done", "2. Development");
            //map.AddReverseEntry("/wip/development/inprocess", "2. Development");
            //map.AddReverseEntry("/wip/development/done", "2. Development");
            //map.AddReverseEntry("/wip/feedback/review", "2. Development");
            //map.AddReverseEntry("/wip/feedback/test", "2. Development");
            //map.AddReciprocalEntry("1. Done", "/done");


            //var creationSynchronizer = new CreationSynchronizer(map, eManagerAgent, whiskWorkAgent);

            //foreach (var synchronizationEntry in eManagerAgent.GetAll())
            //{
            //    Console.WriteLine(synchronizationEntry);
            //}

            //var statusSynchronizer = new StatusSynchronizer(map, whiskWorkAgent, eManagerAgent);

            foreach (var entry in whiskWorkAgent.GetAll())
            {
                Console.WriteLine(entry);
            }

            //creationSynchronizer.Synchronize();
//                statusSynchronizer.Synchronize();
        }
    }
}