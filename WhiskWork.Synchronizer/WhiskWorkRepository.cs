using System;
using System.Collections.Generic;
using System.Net;
using WhiskWork.Core;
using WhiskWork.Web;

namespace WhiskWork.Synchronizer
{
    class WhiskWorkRepository : IWhiskWorkRepository
    {
        private readonly string _site;
        private readonly string _rootPath;

        public WhiskWorkRepository(string site) : this(site,WorkStep.Root.Path)
        {
        }

        public WhiskWorkRepository(string site, string rootPath)
        {
            _site = site;
            _rootPath = rootPath;
        }

        public IEnumerable<WorkItem> GetWorkItems()
        {
            var doc = new WebCommunication().GetXmlDocument(_site + _rootPath);
            return XmlParser.ParseWorkItems(doc);
        }

        public void PostWorkItem(WorkItem workItemUpdate)
        {
            new WebCommunication().PostCsv(_site, workItemUpdate);
        }

        public void DeleteWorkItem(WorkItem workItem)
        {
            var request = (HttpWebRequest)WebRequest.Create(_site + workItem.Path + "/" + workItem.Id);
            request.ContentType = "text/csv";
            request.Method = "delete";

            ((IDisposable)request.GetResponse()).Dispose();
        }
    }
}