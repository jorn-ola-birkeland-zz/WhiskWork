using System;
using System.Collections;
using System.Net;
using WhiskWork.Core;
using WhiskWork.Web;

namespace WhiskWork.WorkItemAger
{
    class Program
    {
        private const string _ageKey = "age";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: WhiskWork.WorkItemAger.exe <webhost[:port]>");
                return;
            }

            var host = args[0];

            var doc = WebCommunication.GetXmlDocument(host + "/");

            Console.WriteLine("Setting age ...");
            foreach (var workItem in XmlParser.ParseWorkItems(doc))
            {
                var ageUpdateWorkItem = GetAgeUpdateWorkItem(workItem);
                if(GetAge(workItem)!=GetAge(ageUpdateWorkItem))
                {
                    Console.WriteLine(ageUpdateWorkItem);
                    SetAge(host, ageUpdateWorkItem);
                }
            }
        }

        private static string GetAge(WorkItem workItem)
        {
            if(workItem.Properties.ContainsKey(_ageKey))
            {
                return workItem.Properties[_ageKey];
            }
            return null;
        }

        private static void SetAge(string host, WorkItem ageUpdateWorkItem)
        {
            try
            {
                WebCommunication.PostCsv(host, ageUpdateWorkItem);
            }
            catch(WebException)
            {
            }
        }

        private static WorkItem GetAgeUpdateWorkItem(WorkItem workItem)
        {
            var now = DateTime.Now;

            var lastMoved = workItem.LastMoved.HasValue ? workItem.LastMoved.Value : now;
            string age;

            if (lastMoved.AddDays(35) < now)
            {
                age = "dead";
            }
            else if (lastMoved.AddDays(20) < now)
            {
                age = "old";
            }
            else if (lastMoved.AddDays(13) < now)
            {
                age = "senior";
            }
            else if (lastMoved.AddDays(8) < now)
            {
                age = "middleaged";
            }
            else if (lastMoved.AddDays(5) < now)
            {
                age = "young";
            }
            else if (lastMoved.AddDays(3) < now)
            {
                age = "adolescent";
            }
            else if (lastMoved.AddDays(2) < now)
            {
                age = "child";
            }
            else if (lastMoved.AddDays(1) < now)
            {
                age = "baby";
            }
            else 
            {
                age = "newborn";
            }

            return WorkItem.New(workItem.Id,workItem.Path).UpdateProperty(_ageKey, age);
        }
    }
}
