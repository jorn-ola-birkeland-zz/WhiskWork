using System;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using DominoInterOp;

namespace WhiskWork.Synchronizer
{
    internal class DominoRepository : IDominoRepository
    {
        private const string _updatePathPattern =
            "/global/seitp/seitp210.ns6/agnLeanUpdate?OpenAgent&amp;UNID={0}&amp;Field={1}&amp;Value={2}&amp;";

        private readonly string _login;
        private readonly string _password;
        private readonly string _dominohost;
        private readonly string _loginUrl;
        private readonly string _viewUrl;
        private DominoAuthenticatingHtmlSource _dominoSource;

        public DominoRepository(string login, string password, string dominohost, string loginUrl, string viewUrl)
        {
            _login = login;
            _password = password;
            _dominohost = dominohost;
            _loginUrl = loginUrl;
            _viewUrl = viewUrl;
        }

        private void SendRequest(string path)
        {
            Open(path).Dispose();
        }

        public void UpdateField(string unid, string fieldName, string fieldValue)
        {
            var field = HttpUtility.UrlEncode(fieldName);
            var value = fieldValue != null ? HttpUtility.UrlEncode(fieldValue, Encoding.GetEncoding("iso-8859-1")) : string.Empty;
            var dataUpdatePath = string.Format(_updatePathPattern, unid, field, value);

            SendRequest(dataUpdatePath);
        }

        public DataTable OpenTable()
        {
            var source = new DominoCleanupHtmlSource(HtmlSource);

            DataTable table;

            using (var reader = source.Open(_viewUrl))
            {
                table = HtmlTableParser.Parse(reader)[0];
            }

            return table;
        }

        private TextReader Open(string path)
        {
            var htmlSource = HtmlSource;

            return htmlSource.Open(path);
        }

        private IHtmlSource HtmlSource
        {
            get
            {
                if (_dominoSource == null)
                {
                    _dominoSource = new DominoAuthenticatingHtmlSource(_dominohost, _loginUrl);
                    _dominoSource.Login(_login, _password);
                }

                return _dominoSource;
            }
        }
    }
}