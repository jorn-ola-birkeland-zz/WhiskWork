using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using WhiskWork.Core.Synchronization;
using WhiskWork.Web;
using WhiskWork.Core;
using System.Linq;

namespace WhiskWork.Synchronizer
{
    class WhiskWorkSynchronizationAgent : ISynchronizationAgent
    {
        private readonly string _site;
        private readonly string _rootPath;
        private readonly string _beginStep;

        public WhiskWorkSynchronizationAgent(string site, string rootPath, string beginStepPath)
        {
            _site = site;
            _rootPath = rootPath;
            _beginStep = beginStepPath;
        }

        public bool IsDryRun { get; set; }

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            XmlDocument doc;
            try
            {
                doc = WebCommunication.GetXmlDocument(_site + _rootPath);
                //doc.Save(@"c:\temp\whiskwork.xml");
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            var workItems = XmlParser.ParseWorkItems(doc, "cr");
            return CreateSynchronizationEntries(workItems);
        }

        private static IEnumerable<SynchronizationEntry> CreateSynchronizationEntries(IEnumerable<WorkItem> workItems)
        {
            var normalCrs = workItems.Where(wi => !wi.Classes.Contains("cr-review") && !wi.Classes.Contains("cr-test"));
            return normalCrs.Select(wi=>SynchronizationEntry.FromWorkItem(wi));
        }

        public void Create(SynchronizationEntry entry)
        {
            Console.WriteLine("WhiskWork.Create:"+entry);

            if (IsDryRun)
            {
                return;
            }

            var payload = CreatePayload(entry);

            PostCsv(payload, _beginStep);

            PostCsv(payload, entry.Status);
        }

        public void Delete(SynchronizationEntry entry)
        {
            Console.WriteLine("WhiskWork.Delete:" + entry);

            if(IsDryRun)
            {
                return;
            }

            var request = (HttpWebRequest)WebRequest.Create(_site + entry.Status + "/" + entry.Id);
            request.ContentType = "text/csv";
            request.Method = "delete";

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(response.StatusCode);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            Console.WriteLine("WhiskWork.Update status:" + entry);

            if (IsDryRun)
            {
                return;
            }

            var payload = new Dictionary<string, string> { { "id", entry.Id } };

            PostCsv(payload, entry.Status);
        }

        public void UpdateData(SynchronizationEntry entry)
        {
            Console.WriteLine("WhiskWork.Update data:" + entry);

            if (IsDryRun)
            {
                return;
            }

            var payload = CreatePayload(entry);

            PostCsv(payload, entry.Status);
        }

        private static IDictionary<string,string> CreatePayload(SynchronizationEntry entry)
        {
            var keyValues = new Dictionary<string, string>();

            keyValues.Add("id",entry.Id);

            if(entry.Ordinal.HasValue)
            {
                keyValues.Add("ordinal", entry.Ordinal.Value.ToString());
            }

            foreach (var keyValuePair in entry.Properties)
            {
                keyValues.Add(keyValuePair.Key,keyValuePair.Value);
            }

            return keyValues;
        }

        private void PostCsv(IDictionary<string, string> payload, string path)
        {
            WebCommunication.PostCsv(_site+path,payload);
        }

    }
}