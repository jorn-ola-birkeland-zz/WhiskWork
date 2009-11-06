namespace WhiskWork.Web
{
    internal static class RequestMessageParserFactory
    {
        public static bool TryCreateParser(string contentType, out IRequestMessageParser parser)
        {
            parser = null;
            switch (contentType.Split(';')[0])
            {
                case "text/csv":
                    parser = new CsvRequestMessageParser();
                    return true;
                case "application/x-www-form-urlencoded":
                    parser = new FormRequestMessageParser();
                    return true;
                default:
                    return false;
            }
        }
    }
}