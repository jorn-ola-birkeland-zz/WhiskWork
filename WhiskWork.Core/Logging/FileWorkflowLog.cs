using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WhiskWork.Generic;

namespace WhiskWork.Core.Logging
{
    public class FileWorkflowLog : IWorkflowLog, IDisposable
    {
        private readonly StreamWriter _writer;
        public FileWorkflowLog(string path)
        {
            var fi = new FileInfo(path);

            if(fi.Directory==null)
            {
                throw new ArgumentException("Invalid path","path");
            }

            if(!fi.Directory.Exists)
            {
                fi.Directory.Create();   
            }

            _writer = new StreamWriter(path, true);
        }

        public void AddLogEntry(string id)
        {
            _writer.WriteLine("{0},{1},{2}", "DELETE ITEM", DateTime.Now, id);
            _writer.Flush();
        }

        public void LogCreateWorkStep(WorkStep workStep)
        {
            _writer.WriteLine("{0},{1},{2}", "CREATE STEP", DateTime.Now, workStep.Path);
            _writer.Flush();
        }

        public void LogCreateWorkItem(WorkItem workItem)
        {

            var serializedProperties = workItem.Properties.Select(kv => kv.Key + "=" + kv.Value).Join('&');
            _writer.WriteLine("{0},{1},{2},{3},{4}", "CREATE ITEM", DateTime.Now, workItem.Id, workItem.Path, serializedProperties);
            _writer.Flush();


        }

        public void LogUpdateWorkItem(WorkItem oldItem, WorkItem newItem)
        {
            var serializedProperties = newItem.Properties.Select(kv => kv.Key + "=" + kv.Value).Join('&');
            _writer.WriteLine("{0},{1},{2},{3},{4},{5}", "UPDATE ITEM", DateTime.Now, oldItem.Id, oldItem.Path, newItem.Path, serializedProperties);
            _writer.Flush();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void AddLogEntry(WorkItemLogEntry logEntry)
        {
            if(logEntry.LogOperation==LogOperationType.Delete)
            {
                _writer.WriteLine("{0},{1},{2}", "DELETE ITEM", logEntry.Timestamp, logEntry.WorkItem.Id);
            }
            else if (logEntry.LogOperation == LogOperationType.Create)
            {
                var serializedProperties = logEntry.WorkItem.Properties.Select(kv => kv.Key + "=" + kv.Value).Join('&');
                _writer.WriteLine("{0},{1},{2},{3},{4}", "CREATE ITEM", logEntry.Timestamp, logEntry.WorkItem.Id, logEntry.WorkItem.Path, serializedProperties);
            }
            else if (logEntry.LogOperation == LogOperationType.Update)
            {
                var serializedProperties = logEntry.WorkItem.Properties.Select(kv => kv.Key + "=" + kv.Value).Join('&');
                _writer.WriteLine("{0},{1},{2},{3},{4},{5}", "UPDATE ITEM", DateTime.Now, logEntry.PreviousWorkItem.Id, logEntry.PreviousWorkItem.Path, logEntry.WorkItem.Path, serializedProperties);
            }

            _writer.Flush();
        }

        public void AddLogEntry(WorkStepLogEntry logEntry)
        {
            _writer.WriteLine("{0},{1},{2}", "CREATE STEP", DateTime.Now, logEntry.WorkStep.Path);
            _writer.Flush();
        }
    }
}