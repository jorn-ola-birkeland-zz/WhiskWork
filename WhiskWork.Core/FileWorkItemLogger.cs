using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WhiskWork.Generic;

namespace WhiskWork.Core
{
    public class FileWorkItemLogger : IWorkItemLogger, IDisposable
    {
        private readonly StreamWriter _writer;
        public FileWorkItemLogger(string path)
        {
            _writer = new StreamWriter(path, true);
        }

        public void LogCreate(WorkItem workItem)
        {
            Log("CREATE", workItem);
        }

        public void LogUpdate(WorkItem oldWorkItem, WorkItem newWorkItem)
        {
            Log("UPDATE", oldWorkItem, newWorkItem);
        }

        public void LogDelete(WorkItem workItem)
        {
            Log("DELETE", workItem);
        }

        private void Log(string operation, WorkItem workItem)
        {
            Log(operation,workItem,workItem);

        }

        private void Log(string operation, WorkItem oldItem, WorkItem newItem)
        {
            //Operation, TimeStamp, Id, Old path, New path, Properties

            var serializedProperties = newItem.Properties.Select(kv => kv.Key + "=" + kv.Value).Join('&');
            _writer.WriteLine("{0},{1},{2},{3},{4},{5}",operation, DateTime.Now,oldItem.Id,oldItem.Path,newItem.Path,serializedProperties);
            _writer.Flush();
            
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}
