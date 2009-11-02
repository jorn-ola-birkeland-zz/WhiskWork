using WhiskWork.Core;
using WhiskWork.Web;

namespace WhiskWork.CommondLineWebServer
{
    internal class HtmlWorkStepRendererFactory : IWorkStepRendererFactory
    {
        private readonly IWorkItemRepository _workItemRepository;
        private readonly IWorkStepRepository _workStepRepository;

        public HtmlWorkStepRendererFactory(IWorkItemRepository workItemRepository, IWorkStepRepository workStepRepository)
        {
            _workItemRepository = workItemRepository;
            _workStepRepository = workStepRepository;
        }

        public IWorkStepRenderer CreateRenderer(string contentType)
        {
            return new HtmlRenderer(_workStepRepository, _workItemRepository);
        }
    }
}