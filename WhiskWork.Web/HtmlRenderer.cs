using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using WhiskWork.Core;
using WhiskWork.Generic;

namespace WhiskWork.Web
{
    public class RootWorkStep : WorkStep
    {
        private RootWorkStep() : base("/", null, 0, WorkStepType.Normal, null)
        {
        }

        public static RootWorkStep Instance
        {
            get
            {
                return new RootWorkStep();
            }
        }
    }

    public class HtmlRenderer
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IWorkItemRepository _workItemRepository;

        public HtmlRenderer(IWorkflowRepository workflowRepository, IWorkItemRepository workItemRepository)
        {
            _workflowRepository = workflowRepository;
            _workItemRepository = workItemRepository;
        }

        public void RenderFull(Stream stream)
        {
            using(var streamWriter = new StreamWriter(stream))
            {
                using(var htmlWriter=new HtmlTextWriter(streamWriter))
                {
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);
                    htmlWriter.RenderBeginTag(HtmlTextWriterTag.Body);

                    WriteStepsRecursively(htmlWriter, RootWorkStep.Instance);

                    htmlWriter.RenderEndTag(); //body
                    htmlWriter.RenderEndTag(); //html
                }
            }
        }

        private void WriteStepsRecursively(HtmlTextWriter writer, WorkStep workStep)
        {
            var query = new WorkStepQuery(_workflowRepository);

            if (query.IsParallelStep(workStep))
            {
                RenderList(writer, workStep, HtmlTextWriterTag.Ul);
            }
            else if(query.IsExpandStep(workStep))
            {
                RenderExpandStep(writer, workStep);
            }
            else
            {
                RenderList(writer,workStep,HtmlTextWriterTag.Ol);
            }
        }

        private void RenderList(HtmlTextWriter writer, WorkStep workStep, HtmlTextWriterTag listTag)
        {
            var childSteps = _workflowRepository.GetChildWorkSteps(workStep.Path);
            if (childSteps.Count() == 0)
            {
                return;
            }

            writer.RenderBeginTag(listTag);
            RenderChildSteps(writer, childSteps);
            writer.RenderEndTag();
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
            var transientSteps = _workflowRepository.GetChildWorkSteps(step.Path).Where(ws => ws.Type == WorkStepType.Transient);

            foreach (var transientStep in transientSteps)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id,GenerateWorkStepId(transientStep));
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "transient");
                writer.RenderBeginTag(HtmlTextWriterTag.Li);

                writer.RenderBeginTag(HtmlTextWriterTag.Ol);

                writer.AddAttribute(HtmlTextWriterAttribute.Class, GetLeafStepClasses(transientStep).Join(' '));
                writer.RenderBeginTag(HtmlTextWriterTag.Li);

                RenderWorkItems(writer,transientStep);

                writer.RenderEndTag();

                var childSteps = _workflowRepository.GetChildWorkSteps(transientStep.Path);
                RenderChildSteps(writer, childSteps);

                writer.RenderEndTag(); //ol

                writer.RenderEndTag(); //li
            }
        }

        private void RenderExpandTemplateListItem(HtmlTextWriter writer, WorkStep workStep)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "expand");
            writer.RenderBeginTag(HtmlTextWriterTag.Li);


            writer.RenderBeginTag(HtmlTextWriterTag.Ol);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, GetLeafStepClasses(workStep).Join(' '));
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            writer.RenderEndTag();

            var childSteps = _workflowRepository.GetChildWorkSteps(workStep.Path).Where(ws=>ws.Type!=WorkStepType.Transient);
            RenderChildSteps(writer, childSteps);

            writer.RenderEndTag(); //ol

            writer.RenderEndTag(); //li
            
        }

        private void RenderChildSteps(HtmlTextWriter writer, IEnumerable<WorkStep> childSteps)
        {
            foreach (var childStep in childSteps.OrderBy(step => step.Ordinal))
            {
                var id = GenerateWorkStepId(childStep);
                writer.AddAttribute(HtmlTextWriterAttribute.Id, id);

                var workStepClass = GenerateWorkStepClass(childStep);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, workStepClass);

                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                RenderTitle(writer, childStep);


                RenderWorkItems(writer, childStep);

                WriteStepsRecursively(writer, childStep);

                writer.RenderEndTag(); //li
            }
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

        private void RenderWorkItems(HtmlTextWriter writer, WorkStep workStep)
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
                writer.RenderEndTag(); //li
            }
            writer.RenderEndTag(); //ol
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
            var query = new WorkStepQuery(_workflowRepository);

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