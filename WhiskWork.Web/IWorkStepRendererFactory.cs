namespace WhiskWork.Web
{
    public interface IWorkStepRendererFactory
    {
        IWorkStepRenderer CreateRenderer(string contentType);
    }
}