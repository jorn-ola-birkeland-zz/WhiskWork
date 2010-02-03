using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WhiskWork.Core;
using WhiskWork.Core.Synchronization;

namespace WhiskWork.Synchronizer
{
    public class BugSynchronizer : EManagerWhiskWorkSynchronizer
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


        private const string _portal="1";
        private const string _portalAdmin = "14";
        private const string _preview = "17";
        private const string _feedback = "6";
        private const string _references = "5";
        private const string _webEditing = "15";
        private const string _search = "34";
        private const string _abbUniversity = "7";


        private const int _undefinedOrdinal = 10000;

        public BugSynchronizer(IWhiskWorkRepository whiskWorkRepository, IDominoRepository dominoRepository) : base(whiskWorkRepository, dominoRepository)
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

            statusMap.AddReciprocalEntry(_inProcess, "/cmsdev/analysis/inprocess");
            statusMap.AddReverseEntry("/cmsdev/analysis/done", _inProcess);
            statusMap.AddReverseEntry("/cmsdev/development/inprocess", _inProcess);
            statusMap.AddReciprocalEntry(_readyForTest, "/cmsdev/development/done");
            statusMap.AddReverseEntry("/cmsdev/feedback", _readyForTest);
            statusMap.AddReverseEntry("/cmsdev/feedback/review", _readyForTest);
            statusMap.AddReverseEntry("/cmsdev/feedback/test", _readyForTest);
            
            statusMap.AddReciprocalEntry(_approvedInDev, "/done");
            statusMap.AddForwardEntry(_failed, "/cmsdev/development/inprocess");
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
            var bugs = workItems.Where(wi => wi.Classes.Contains("cr") && wi.Id.StartsWith("B"));
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
            var priority = GetPriority((string)row[6]);
            var severity = (string)row[7];
            var timeStamp = ParseDominoTimeStamp((string)row[8]);

            if ((ApplicationIdFilter != null && !ApplicationIdFilter.Contains(applicationId)) || (ReleaseFilter != null && !ReleaseFilter.Contains(release)))
            {
                return null;
            }

            if (string.IsNullOrEmpty(id) || id == "0")
            {
                return null;
            }


            id = "B" + id;

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
                        {"type","bug"},
                    };

            var ordinal = !priority.HasValue ? -1 : -4+priority;
            return new SynchronizationEntry(id, status, properties) { Ordinal = ordinal, TimeStamp=timeStamp};
        }

        private static int? GetPriority(string rowItem)
        {
            if (string.IsNullOrEmpty(rowItem))
            {
                return null;
            }

            int value;
            if (!int.TryParse(rowItem, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en"), out value))
            {
                return null;
            }

            return value;
        }

    }
}
