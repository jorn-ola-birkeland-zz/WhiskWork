using System.IO;

namespace WhiskWork.Web
{
    public interface IWorkStepRenderer
    {
        void Render(Stream stream, string path);
        string ContentType { get; }
    }
}