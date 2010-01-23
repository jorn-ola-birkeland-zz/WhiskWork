#region

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using DominoInterOp;
using WhiskWork.Core.Synchronization;
using System.Web;
using System.Linq;

#endregion

namespace WhiskWork.Synchronizer
{
    internal class EManagerSynchronizationAgent : ISynchronizationAgent
    {
        private readonly string _login;
        private readonly string _password;
        private readonly string _dominohost;
        private DominoAuthenticatingHtmlSource _dominoSource;

        private const int _undefinedOrdinal = 10000;

        public EManagerSynchronizationAgent(string dominohost, string login, string password)
        {
            _dominohost = dominohost;
            _login = login;
            _password = password;
        }

        public IEnumerable<string> Release { get; set; }
        public IEnumerable<string> Team { get; set; }

        public bool IsDryRun { get; set; }

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
                var ordinal = GetOrdinal((string)row[8]);
                var person = (string) row[9];

                if ((Team!=null && !Team.Contains(team)) || (Release!=null && !Release.Contains(release)))
                {
                    continue;
                }

                if(string.IsNullOrEmpty(id) || id=="0")
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
                            {"priority",ordinal==_undefinedOrdinal ? "undefined" : ordinal.ToString()},
                        };

                if(!string.IsNullOrEmpty(person))
                {
                    properties.Add("CurrentPerson",person);
                }


                var entry = new SynchronizationEntry(id, status, properties) {Ordinal = ordinal};
                entries.Add(entry);
            }

            return entries;
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            var unid = entry.Properties["unid"];

            var updatePathPattern = ConfigurationManager.AppSettings["updatePathPattern"];

            var statusUpdatePath = string.Format(updatePathPattern,unid,"Status",HttpUtility.UrlEncode(entry.Status));

            Console.WriteLine("EManager. Update status: "+statusUpdatePath);

            if (!IsDryRun)
            {
                var dominoSource = Login();
                dominoSource.Open(statusUpdatePath).Dispose();
            }
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
            var unid = entry.Properties["unid"];

            var updatePathPattern = ConfigurationManager.AppSettings["updatePathPattern"];

            foreach (var keyValue in entry.Properties)
            {
                if(keyValue.Key=="unid")
                {
                    continue;
                }

                var key = HttpUtility.UrlEncode(keyValue.Key);
                var value = keyValue.Value!=null ? HttpUtility.UrlEncode(keyValue.Value, Encoding.GetEncoding("iso-8859-1")) : string.Empty ;
                var dataUpdatePath = string.Format(updatePathPattern, unid, key, value);

                Console.WriteLine("EManager. Update data: "+dataUpdatePath);

                if(!IsDryRun)
                {
                    var dominoSource = Login();
                    dominoSource.Open(dataUpdatePath).Dispose();
                }
            }
        }

        #endregion

        private DominoAuthenticatingHtmlSource Login()
        {
            if(_dominoSource!=null)
            {
                return _dominoSource;
            }
            var loginUrl = ConfigurationManager.AppSettings["loginUrl"];

            _dominoSource = new DominoAuthenticatingHtmlSource(_dominohost, loginUrl);
            _dominoSource.Login(_login, _password);
            return _dominoSource;
        }

        private static int GetOrdinal(string rowItem)
        {
            if (string.IsNullOrEmpty(rowItem))
            {
                return _undefinedOrdinal;
            }

            decimal value;
            if(!decimal.TryParse(rowItem, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en"), out value))
            {
                return _undefinedOrdinal;
            }

            return Convert.ToInt32(Math.Round(value));
        }

    }
}