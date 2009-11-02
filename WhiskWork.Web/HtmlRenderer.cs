using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using WhiskWork.Core;
using WhiskWork.Generic;

namespace WhiskWork.Web
{
    public class HtmlRenderer : IWorkStepRenderer
    {
        private readonly IWorkStepRepository _workStepRepository;
        private readonly IWorkItemRepository _workItemRepository;

        public HtmlRenderer(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository)
        {
            _workStepRepository = workStepRepository;
            _workItemRepository = workItemRepository;
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
                var workStep = _workStepRepository.GetWorkStep(path);

                Render(stream, workStep);
            }
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
            RenderWorkItemList(writer, workStep);

            var query = new WorkStepQuery(_workStepRepository);

            if (query.IsParallelStep(workStep))
            {
                RenderParallelStep(writer, workStep);
            }
            else if(query.IsExpandStep(workStep))
            {
                RenderExpandStep(writer, workStep);
            }
            else
            {
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
            var childSteps = _workStepRepository.GetChildWorkSteps(workStep.Path);
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
            if(!new WorkStepQuery(_workStepRepository).IsExpandStep(workStep) )
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
            RenderExpandTransientListItems(writer, workStep);
            RenderExpandTemplateListItem(writer, workStep);

            writer.RenderEndTag(); //ol
        }

        private void RenderExpandTransientListItems(HtmlTextWriter writer, WorkStep step)
        {
            var transientSteps = _workStepRepository.GetChildWorkSteps(step.Path).Where(ws => ws.Type == WorkStepType.Transient);

            foreach (var transientStep in transientSteps)
            {
                RenderExpandTransientListItem(writer,transientStep);
            }
        }

        private void RenderExpandTransientListItem(HtmlTextWriter writer, WorkStep transientStep)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "transient");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            writer.RenderBeginTag(HtmlTextWriterTag.Ol);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, GenerateWorkStepId(transientStep));
            writer.AddAttribute(HtmlTextWriterAttribute.Class, GetLeafStepClasses(transientStep).Join(' '));
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            RenderWorkItemList(writer, transientStep);

            writer.RenderEndTag(); //li

            var childSteps = _workStepRepository.GetChildWorkSteps(transientStep.Path);
            RenderWorkStepListItems(writer, childSteps);

            writer.RenderEndTag(); //ol

            writer.RenderEndTag(); //li
        }

        private void RenderExpandTemplateListItem(HtmlTextWriter writer, WorkStep workStep)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "expand");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);


            writer.RenderBeginTag(HtmlTextWriterTag.Ol);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, GenerateWorkStepId(workStep));
            writer.AddAttribute(HtmlTextWriterAttribute.Class, GetLeafStepClasses(workStep).Join(' '));
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.RenderEndTag();

            var childSteps = _workStepRepository.GetChildWorkSteps(workStep.Path).Where(ws=>ws.Type!=WorkStepType.Transient);
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
            writer.Write(step.Title);
            writer.RenderEndTag(); //h1
        }

        private void RenderWorkItemList(HtmlTextWriter writer, WorkStep workStep)
        {
            var workItems = _workItemRepository.GetWorkItems(workStep.Path).Where(wi=>wi.Status!=WorkItemStatus.ParallelLocked);
            if(workItems.Count()==0)
            {
                return;
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Ol);
            foreach (var workItem in workItems.OrderBy(wi => wi.Ordinal))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, workItem.Id);

                var workItemClass = GenerateWorkItemClass(workItem);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, workItemClass);
                writer.RenderBeginTag(HtmlTextWriterTag.Li);

                RenderProperties(writer, workItem);

                writer.RenderEndTag(); //li
            }
            writer.RenderEndTag(); //ol
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
                writer.AddAttribute(HtmlTextWriterAttribute.Class, key.ToLowerInvariant());
                writer.RenderBeginTag(HtmlTextWriterTag.Dt);
                writer.Write(key);
                writer.RenderEndTag();

                writer.AddAttribute(HtmlTextWriterAttribute.Class, key.ToLowerInvariant());
                writer.RenderBeginTag(HtmlTextWriterTag.Dd);
                writer.Write(item.Properties[key]);
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
            var query = new WorkStepQuery(_workStepRepository);

            if(!query.IsExpandStep(workStep) && query.IsLeafStep(workStep))
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