using System.Collections.Generic;
using System.IO;

namespace WhiskWork.Web
{
    internal interface IRequestMessageParser
    {
        IWorkflowNode Parse(Stream messageStream);
    }
}