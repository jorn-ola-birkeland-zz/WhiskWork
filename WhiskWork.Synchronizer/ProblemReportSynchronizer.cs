using System;
using System.Collections.Generic;
using System.Data; 
using System.Linq;
using WhiskWork.Core;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Synchronizer
{
    public class ProblemReportSynchronizer : EManagerWhiskWorkSynchronizer
    {
        private const string _new = "1";
        private const string _inProcess = "2";
        private const string _readyForTest = "3";
        private const string _failed = "9";
        private const string _approvedInDev = "10";
        private const string _approvedInTest = "11";
        private const string _approvedInStage = "12";
        private const string _approvedInProduction = "13";
        private const string _closed = "5";

        private const string _typeBug = "1";
        private const string _typeTech = "2";
        private const string _typeSupport = "3";

        public ProblemReportSynchronizer(IWhiskWorkRepository whiskWorkRepository, IDominoRepository dominoRepository) : base(whiskWorkRepository, dominoRepository)
        {
        }

        public IEnumerable<string> ReleaseFilter { get; set; }
        public IEnumerable<string> ApplicationIdFilter { get; set; }

        protected override string WhiskWorkBeginStep
        {
            get { return "/cmsdev/scheduled"; }
        }

        protected override bool SynchronizeResponsibleEnabled
        {
            get { return false; }
        }

        protected override bool SynchronizeStatusReverseEnabled
        {
            get { return true; }
        }

        protected override SynchronizationMap CreateStatusMap()
        {
            var statusMap = new SynchronizationMap(EManagerAgent, WhiskWorkAgent);
            statusMap.AddReciprocalEntry(_new, WhiskWorkBeginStep);

            statusMap.AddReciprocalEntry(_inProcess, "/cmsdev/wip/analysis/inprocess");
            statusMap.AddReverseEntry("/cmsdev/wip/analysis/done", _inProcess);
            statusMap.AddReverseEntry("/cmsdev/wip/development/inprocess", _inProcess);
            statusMap.AddReciprocalEntry(_readyForTest, "/cmsdev/wip/development/done");
            statusMap.AddReverseEntry("/cmsdev/wip/feedback", _readyForTest);
            statusMap.AddReverseEntry("/cmsdev/wip/feedback/review", _readyForTest);
            statusMap.AddReverseEntry("/cmsdev/wip/feedback/test", _readyForTest);
            
            statusMap.AddReciprocalEntry(_approvedInDev, "/done");
            statusMap.AddForwardEntry(_failed, "/cmsdev/wip/development/inprocess");
            statusMap.AddReverseEntry("/test/inprocess", _approvedInDev);
            statusMap.AddReciprocalEntry(_approvedInTest, "/test/done");
            statusMap.AddReverseEntry("/stage", _approvedInTest);
            statusMap.AddReciprocalEntry(_approvedInStage, "/approved");
            statusMap.AddReciprocalEntry(_closed, "/deployed");

            return statusMap;
        }

        protected override DataSynchronizer CreatePropertyMap()
        {
            var propertyMap = new SynchronizationMap(EManagerAgent, WhiskWorkAgent);
            propertyMap.AddReciprocalEntry("name", "name");
            propertyMap.AddReciprocalEntry("unid", "unid");
            propertyMap.AddReciprocalEntry("title", "title");
            propertyMap.AddReciprocalEntry("applicationid", "applicationid");
            propertyMap.AddReciprocalEntry("release", "release");
            propertyMap.AddReciprocalEntry("severity", "severity");
            propertyMap.AddReciprocalEntry("priority", "priority");
            propertyMap.AddReciprocalEntry("type", "type");

            var propertySynchronizer = new DataSynchronizer(propertyMap, EManagerAgent, WhiskWorkAgent);
            propertySynchronizer.SynchronizeOrdinal = true;
            return propertySynchronizer;
        }

        protected override IEnumerable<SynchronizationEntry> MapFromWhiskWork(IEnumerable<WorkItem> workItems)
        {
            var bugs = workItems.Where(wi => wi.Classes.Contains("cr") && (wi.Id.StartsWith("B") || wi.Id.StartsWith("S") || wi.Id.StartsWith("T")));
            var normalBugs = bugs.Where(wi => !wi.Classes.Contains("cr-review") && !wi.Classes.Contains("cr-test"));
            return normalBugs.Select(wi => SynchronizationEntry.FromWorkItem(wi));
        }

        protected override SynchronizationEntry MapFromEManager(DataRow row)
        {
            var release = (string)row[0];
            var applicationId = (string)row[1];
            var id = (string)row[2];
            var title = (string)row[3];
            var unid = (string)row[4];
            var status = (string)row[5];
            var priority = ParseNumber((string)row[6]);
            var severity = (string)row[7];
            var timeStamp = ParseDominoTimeStamp((string)row[8]);
            var type = GetProblemReportType((string) row[9]);

            if ((ApplicationIdFilter != null && !ApplicationIdFilter.Contains(applicationId)) || (ReleaseFilter != null && !ReleaseFilter.Contains(release)))
            {
                return null;
            }

            if (string.IsNullOrEmpty(id) || id == "0")
            {
                return null;
            }

            id = type.Substring(0,1).ToUpperInvariant() + id;

            var properties =
                new Dictionary<string, string>
                    {
                        {"name", id},
                        {"unid", unid},
                        {"title", title},
                        {"applicationid", applicationId},
                        {"release", release},
                        {"severity",severity},
                        {"priority",!priority.HasValue ? "undefined" : priority.ToString()},
                        {"type",type},
                    };

            var ordinal = !priority.HasValue ? -1 : -4+priority;
            return new SynchronizationEntry(id, status, properties) { Ordinal = ordinal, TimeStamp=timeStamp};
        }

        private static string GetProblemReportType(string value)
        {
            if (value == _typeSupport)
            {
                return "support";
            }
            if (value == _typeTech)
            {
                return "technical";
            }
            
            return "bug";
        }
    }
}
