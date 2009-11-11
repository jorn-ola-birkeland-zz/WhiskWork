#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using DominoInterOp;
using WhiskWork.Core.Synchronization;
using System.Web;

#endregion

namespace WhiskWork.Synchronizer
{
    internal class EManagerSynchronizationAgent : ISynchronizationAgent
    {
        private readonly string _release;
        private readonly string _team;

        public EManagerSynchronizationAgent(string release, string team)
        {
            _release = release;
            _team = team;
        }

        #region ISynchronizationAgent Members

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            var dominoSource = Login();

            var eManagerUrl = ConfigurationManager.AppSettings["leanViewUrl"];

            var source = new DominoCleanupHtmlSource(dominoSource);

            DataTable table;

            using (var reader = source.Open(eManagerUrl))
            {
                table = HtmlTableParser.Parse(reader)[0];
            }

            var entries = new List<SynchronizationEntry>();
            foreach (DataRow row in table.Rows)
            {
                var release = (string)row[0];
                var leanStatus = (string)row[1];
                var team = (string)row[2];
                var id = (string)row[3];
                var title = (string)row[4];
                var project = (string) row[5];
                var unid = (string) row[6];
                var status = (string)row[7];
                int? ordinal = ToNullableInt((string)row[8]);

                if (team != _team || release != _release)
                {
                    continue;
                }

                var properties =
                    new Dictionary<string, string>
                        {
                            {"name", id},
                            {"unid", unid},
                            {"title", title},
                            {"team", team},
                            {"release", release},
                            {"project", project},
                            {"leanstatus",leanStatus},
                            {"priority",ordinal.HasValue ? ordinal.Value.ToString() : string.Empty}
                        };


                var entry = new SynchronizationEntry(id, status, properties) {Ordinal = ordinal};
                entries.Add(entry);
            }

            return entries;
        }



        public void UpdateStatus(SynchronizationEntry entry)
        {
            var dominoSource = Login();

            var unid = entry.Properties["unid"];

            var updateStatusPathPattern = ConfigurationManager.AppSettings["updateStatusPathPattern"];

            var statusUpdatePath = string.Format(updateStatusPathPattern,unid,HttpUtility.UrlEncode(entry.Status));

            dominoSource.Open(statusUpdatePath);
        }

        public void Create(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void Delete(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void UpdateData(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        #endregion

        private static DominoAuthenticatingHtmlSource Login()
        {
            var host = ConfigurationManager.AppSettings["host"];
            var loginUrl = ConfigurationManager.AppSettings["loginUrl"];

            var username = ConfigurationManager.AppSettings["login"];
            var password = ConfigurationManager.AppSettings["password"];

            var dominoSource = new DominoAuthenticatingHtmlSource(host, loginUrl);
            dominoSource.Login(username, password);
            return dominoSource;
        }

        private static int? ToNullableInt(string rowItem)
        {
            if (string.IsNullOrEmpty(rowItem))
            {
                return null;
            }

            decimal value;
            if(!decimal.TryParse(rowItem, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en"), out value))
            {
                return null;
            }

            return Convert.ToInt32(Math.Round(value));
        }

    }
}