using System.IO;

namespace WhiskWork.Generic
{
    internal static class StreamUtil
    {
        public static void CopyStream(Stream fromStream, Stream toStream)
        {
            var buffer = new byte[1024];
            int readBytes;

            while ((readBytes = fromStream.Read(buffer, 0, 1024)) > 0)
            {
                toStream.Write(buffer, 0, readBytes);
            }
        }
    }
}