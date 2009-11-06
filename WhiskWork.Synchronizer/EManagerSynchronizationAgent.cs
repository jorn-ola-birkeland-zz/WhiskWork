#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using DominoInterOp;
using WhiskWork.Core.Synchronization;

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
            var host = ConfigurationManager.AppSettings["host"];
            var loginUrl = ConfigurationManager.AppSettings["loginUrl"];
            var eManagerUrl = ConfigurationManager.AppSettings["timesheetUrl"];

            var username = ConfigurationManager.AppSettings["login"];
            var password = ConfigurationManager.AppSettings["password"];

            var dominoSource = new DominoAuthenticatingHtmlSource(host, loginUrl);
            dominoSource.Login(username, password);

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
                            {"status",status}
                        };


                entries.Add(new SynchronizationEntry(id, leanStatus, properties));
            }

            return entries;
        }

        public void Create(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void Delete(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void UpdateProperties(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}