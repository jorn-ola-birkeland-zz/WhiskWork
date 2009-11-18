using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using WhiskWork.Core.Synchronization;

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

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            var request = (HttpWebRequest)WebRequest.Create(_site + _rootPath);

            //request.ContentType = "text/xml";
            request.Accept = "text/xml";
            request.Method = "GET";

            var doc = new XmlDocument();
            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                doc.Load(response.GetResponseStream());

                //doc.Save(@"c:\temp\whiskwork.xml");

                //Console.WriteLine(doc.InnerXml);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }

            var worksteps = doc.SelectNodes("//WorkStep[@workItemClass='cr']");

            if (worksteps == null)
            {
                yield break;
            }

            foreach (XmlNode workstep in worksteps)
            {
                var workStepId = workstep.SelectSingleNode("@id").Value;
                var workStepPath = "/" + workStepId.Replace('.', '/');
                var workItems = workstep.SelectNodes("WorkItems/WorkItem");

                if (workItems == null)
                {
                    continue;
                }

                foreach (XmlNode workItem in workItems)
                {
                    var workItemId = workItem.SelectSingleNode("@id").Value;
                    var workItemClasses = workItem.SelectSingleNode("@classes").Value;

                    //Exclude children of parallelled work-items. Should add property and move out
                    if(workItemClasses.Contains("cr-review") || workItemClasses.Contains("cr-test"))
                    {
                        continue;
                    }

                    var properties = CreateProperties(workItem);
                    var synchronizationEntry = new SynchronizationEntry(workItemId,workStepPath,properties);

                    var ordinal = XmlConvert.ToInt32(workItem.SelectSingleNode("@ordinal").Value);
                    synchronizationEntry.Ordinal = ordinal;

                    yield return synchronizationEntry;
                }
            }
        }

        private static Dictionary<string,string> CreateProperties(XmlNode workItem)
        {
            var propertyNodes = workItem.SelectNodes("Properties/Property");

            var properties = new Dictionary<string, string>();

            if(propertyNodes==null)
            {
                return properties;
            }

            foreach (XmlNode propertyNode in propertyNodes)
            {
                var key = propertyNode.SelectSingleNode("@name").Value;
                var value = propertyNode.InnerText;

                properties.Add(key,value);
            }

            return properties;
        }

        public void Create(SynchronizationEntry entry)
        {
            Console.WriteLine("WhiskWork.Create:"+entry);
            var payload = CreatePayload(entry);

            PostCsv(payload, _beginStep);

            PostCsv(payload, entry.Status);
        }

        public void Delete(SynchronizationEntry entry)
        {
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
            var payload = string.Format("id={0}", entry.Id);

            PostCsv(payload, entry.Status);
        }

        public void UpdateData(SynchronizationEntry entry)
        {
            var payload = CreatePayload(entry);

            PostCsv(payload, entry.Status);
        }

        private static string CreatePayload(SynchronizationEntry entry)
        {
            var payloadBuilder = new StringBuilder();
            payloadBuilder.AppendFormat("id={0}", entry.Id);

            if(entry.Ordinal.HasValue)
            {
                payloadBuilder.AppendFormat(",ordinal={0}", entry.Ordinal.Value);
            }

            foreach (var keyValuePair in entry.Properties)
            {
                payloadBuilder.AppendFormat(",{0}={1}", HttpUtility.HtmlEncode(keyValuePair.Key), HttpUtility.HtmlEncode(keyValuePair.Value));
            }
            return payloadBuilder.ToString();
        }

        private void PostCsv(string payload, string path)
        {
            var request = (HttpWebRequest)WebRequest.Create(_site + path);
            request.ContentType = "text/csv";
            request.Method = "post";
            request.ContentLength = payload.Length;

            try
            {
                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(payload);
                }

                var response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}