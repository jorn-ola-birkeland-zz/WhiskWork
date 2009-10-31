namespace WhiskWork.Web
{
    internal static class RequestMessageParserFactory
    {
        public static bool TryCreate(string contentType, out IRequestMessageParser parser)
        {
            parser = null;
            switch (contentType)
            {
                case "text/csv":
                    parser = new CsvRequestMessageParser();
                    return true;
                default:
                    return false;
            }
        }
    }
}