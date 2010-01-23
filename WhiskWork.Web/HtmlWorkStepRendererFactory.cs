using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class HtmlWorkStepRendererFactory : IWorkStepRendererFactory
    {
        private readonly IWorkflow _workflowRepository;

        public HtmlWorkStepRendererFactory(IWorkflow workflowRepository)
        {
            _workflowRepository = workflowRepository;
        }

        public IWorkStepRenderer CreateRenderer(string contentType)
        {
            switch(contentType)
            {
                case "text/xml":
                    return new XmlRenderer(_workflowRepository);
                case "application/json":
                    return new JsonRenderer(_workflowRepository);
                default:
                    return new HtmlRenderer(_workflowRepository);
            }
        }
    }
}