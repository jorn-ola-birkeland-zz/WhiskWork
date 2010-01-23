using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using WhiskWork.Core;
using WhiskWork.Generic;

namespace WhiskWork.Web
{
    public class HtmlRenderer : IWorkStepRenderer
    {
        private readonly IReadableWorkflowRepository _workflowRepository;

        public HtmlRenderer(IReadableWorkflowRepository workflowRepository)
        {
            _workflowRepository = workflowRepository;
        }

        public void Render(Stream stream)
        {
            Render(stream, WorkStep.Root);
        }

        public void Render(Stream stream, string path)
        {
            if(string.IsNullOrEmpty(path) || WorkStep.Root.Path==path)
            {
                Render(stream, WorkStep.Root);
            }
            else
            {
                var workStep = _workflowRepository.GetWorkStep(path);

                Render(stream, workStep);
            }
        }

        public string ContentType
        {
            get { return "text/html"; }
        }


        public void Render(Stream stream, WorkStep workStep)
        {
            using (var streamWriter = new StreamWriter(stream))
            {
                using (var htmlWriter = new HtmlTextWriter(streamWriter))
                {
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);

                    RenderWorkStepsRecursively(htmlWriter, workStep);

                    htmlWriter.RenderEndTag(); //body
                    htmlWriter.RenderEndTag(); //html
                }
            }
        }


        private void RenderWorkStepsRecursively(HtmlTextWriter writer, WorkStep workStep)
        {

            if (_workflowRepository.IsParallelStep(workStep))
            {
                RenderParallelStep(writer, workStep);
            }
            else if (_workflowRepository.IsExpandStep(workStep))
            {
                RenderExpandStep(writer, workStep);
            }
            else
            {
                RenderWorkItemList(writer, workStep);
                RenderNormalStep(writer, workStep);
            }
        }


        private void RenderParallelStep(HtmlTextWriter writer, WorkStep workStep)
        {
            RenderNonEmptyWorkStepList(writer, workStep, HtmlTextWriterTag.Ul);
        }

        private void RenderNormalStep(HtmlTextWriter writer, WorkStep workStep)
        {
            RenderNonEmptyWorkStepList(writer, workStep, HtmlTextWriterTag.Ol);
        }

        private void RenderNonEmptyWorkStepList(HtmlTextWriter writer, WorkStep workStep, HtmlTextWriterTag listTag)
        {
            var childSteps = _workflowRepository.GetChildWorkSteps(workStep.Path);
            if (childSteps.Count() == 0)
            {
                //RenderWorkItemList(writer,workStep);
                return;
            }

            writer.RenderBeginTag(listTag);
            RenderWorkStepListItems(writer, childSteps);
            writer.RenderEndTag();
        }

        private void RenderWorkStepListItems(HtmlTextWriter writer, IEnumerable<WorkStep> workSteps)
        {
            foreach (var workStep in workSteps.OrderBy(step => step.Ordinal))
            {
                RenderWorkStepListItem(writer, workStep);
            }
        }

        private void RenderWorkStepListItem(HtmlTextWriter writer, WorkStep workStep)
        {
            if (!_workflowRepository.IsExpandStep(workStep))
            {
                var id = GenerateWorkStepId(workStep);
                writer.AddAttribute(HtmlTextWriterAttribute.Id, id);
            }

            var workStepClass = GenerateWorkStepClass(workStep);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, workStepClass);

            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            RenderTitle(writer, workStep);

            RenderWorkStepsRecursively(writer, workStep);

            writer.RenderEndTag(); //li

        }


        private void RenderExpandStep(HtmlTextWriter writer, WorkStep workStep)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Ol);
            RenderTransientListItems(writer, workStep);
            RenderExpandListItem(writer, workStep);

            writer.RenderEndTag(); //ol
        }

        private void RenderTransientListItems(HtmlTextWriter writer, WorkStep expandStep)
        {
            var expandedWorkItems = _workflowRepository.GetWorkItems(expandStep.Path).OrderBy(wi => wi.Ordinal);

            foreach (var expandedWorkItem in expandedWorkItems)
            {
                var transientPath = ExpandedWorkStep.GetTransientPath(expandStep, expandedWorkItem);
                var transientStep = _workflowRepository.GetWorkStep(transientPath);

                RenderTransientListItem(writer, transientStep, expandedWorkItem);
            }

        }

        private void RenderTransientListItem(HtmlTextWriter writer, WorkStep transientStep, WorkItem expandedWorkItem)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "transient");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            writer.RenderBeginTag(HtmlTextWriterTag.Ol);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, GenerateWorkStepId(transientStep));
            writer.AddAttribute(HtmlTextWriterAttribute.Class, GetLeafStepClasses(transientStep).Join(' '));
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            writer.RenderBeginTag(HtmlTextWriterTag.Ol);
            RenderWorkItem(writer, expandedWorkItem);
            writer.RenderEndTag(); //ol

            writer.RenderEndTag(); //li

            var childSteps = _workflowRepository.GetChildWorkSteps(transientStep.Path);
            RenderWorkStepListItems(writer, childSteps);

            writer.RenderEndTag(); //ol

            writer.RenderEndTag(); //li
        }

        private void RenderExpandListItem(HtmlTextWriter writer, WorkStep workStep)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "expand");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);


            writer.RenderBeginTag(HtmlTextWriterTag.Ol);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, GenerateWorkStepId(workStep));
            writer.AddAttribute(HtmlTextWriterAttribute.Class, GetLeafStepClasses(workStep).Join(' '));
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.RenderEndTag();

            var childSteps = _workflowRepository.GetChildWorkSteps(workStep.Path).Where(ws => ws.Type != WorkStepType.Transient);
            RenderWorkStepListItems(writer, childSteps);

            writer.RenderEndTag(); //ol

            writer.RenderEndTag(); //li
            
        }


        private static void RenderTitle(HtmlTextWriter writer, WorkStep step)
        {
            if(string.IsNullOrEmpty(step.Title))
            {
                return;
            }

            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            writer.Write(HttpUtility.HtmlEncode(step.Title));
            writer.RenderEndTag(); //h1
        }

        private void RenderWorkItemList(HtmlTextWriter writer, WorkStep workStep)
        {
            var workItems = _workflowRepository.GetWorkItems(workStep.Path).Where(wi => wi.Status != WorkItemStatus.ParallelLocked);
            if(workItems.Count()==0)
            {
                return;
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Ol);
            foreach (var workItem in workItems.OrderBy(wi => wi.Ordinal))
            {
                RenderWorkItem(writer, workItem);
            }
            writer.RenderEndTag(); //ol
        }

        private static void RenderWorkItem(HtmlTextWriter writer, WorkItem workItem)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, workItem.Id);

            var workItemClass = GenerateWorkItemClass(workItem);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, workItemClass);
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            RenderProperties(writer, workItem);

            writer.RenderEndTag(); //li
            
        }

        private static void RenderProperties(HtmlTextWriter writer, WorkItem item)
        {
            if(item.Properties.Count==0)
            {
                return;
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "properties");
            writer.RenderBeginTag(HtmlTextWriterTag.Dl);

            foreach (var key in item.Properties.AllKeys)
            {
                var proprtyClass = HttpUtility.HtmlEncode(key.ToLowerInvariant());
                writer.AddAttribute(HtmlTextWriterAttribute.Class, proprtyClass);
                writer.RenderBeginTag(HtmlTextWriterTag.Dt);
                writer.Write(key);
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, proprtyClass);
                writer.RenderBeginTag(HtmlTextWriterTag.Dd);
                writer.Write(HttpUtility.HtmlEncode(item.Properties[key]));
                writer.RenderEndTag();
            }

            writer.RenderEndTag();
        }

        private static string GenerateWorkItemClass(WorkItem item)
        {
            var classes = new List<string> {"workitem"};
            classes.AddRange(item.Classes);

            return classes.Join(' ');
        }

        private string GenerateWorkStepClass(WorkStep workStep)
        {
            var classes = new List<string>();

            if (!_workflowRepository.IsExpandStep(workStep) && _workflowRepository.IsLeafStep(workStep))
            {
                classes.AddRange(GetLeafStepClasses(workStep));
            }

            classes.Add(GetLeafDirectory(workStep));

            return classes.Join(' ');
        }

        private static IEnumerable<string> GetLeafStepClasses(WorkStep workStep)
        {
            yield return "workstep";
            yield return GetWorkItemClassForWorkStep(workStep);
        }

        private static string GetLeafDirectory(WorkStep workStep)
        {
            return workStep.Path.Split('/').Last();
        }

        private static string GetWorkItemClassForWorkStep(WorkStep workStep)
        {
            return string.Format("step-{0}", workStep.WorkItemClass);
        }

        private static string GenerateWorkStepId(WorkStep step)
        {
            return step.Path.Remove(0, 1).Replace('/','.');
        }
    }
}