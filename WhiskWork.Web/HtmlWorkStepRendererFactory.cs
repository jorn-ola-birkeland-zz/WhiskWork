using System;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class HtmlWorkStepRendererFactory : IWorkStepRendererFactory
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
            switch(contentType)
            {
                case "application/json":
                    return new JsonRenderer(_workStepRepository, _workItemRepository);
                default:
                    return new HtmlRenderer(_workStepRepository, _workItemRepository);
            }
        }
    }
}