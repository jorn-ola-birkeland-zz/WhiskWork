using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using WhiskWork.Core;
using WhiskWork.Generic;

namespace WhiskWork.Web
{
    public class HtmlRenderer
    {
        private readonly IWorkflowRepository _workflowRepository;
        private IWorkItemRepository _workItemRepository;

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

                    WriteStepsRecursively(htmlWriter, "/");

                    htmlWriter.RenderEndTag(); //body
                    htmlWriter.RenderEndTag(); //html
                }
            }
        }

        private void WriteStepsRecursively(HtmlTextWriter writer, string path)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Ol);
            foreach (var workStep in _workflowRepository.GetChildWorkSteps(path).OrderBy(step => step.Ordinal))
            {
                string id = GenerateWorkStepId(workStep);
                writer.AddAttribute(HtmlTextWriterAttribute.Id,id);

                string workstep = GenerateWorkStepClasses(workStep);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, workstep);

                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.RenderBeginTag(HtmlTextWriterTag.H1);
                writer.Write(workStep.Title);
                writer.RenderEndTag(); //h1


                RenderWorkItem(writer, workStep);

                WriteStepsRecursively(writer, workStep.Path);

                writer.RenderEndTag(); //li
            }
            writer.RenderEndTag(); //ol
        }

        private void RenderWorkItem(HtmlTextWriter writer, WorkStep workStep)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Ol);
            foreach (var workItem in _workItemRepository.GetWorkItems(workStep.Path).OrderBy(wi => wi.Ordinal))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, workItem.Id);

                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.RenderEndTag(); //li
            }
            writer.RenderEndTag(); //ol
        }

        private string GenerateWorkStepClasses(WorkStep workStep)
        {
            var classes = new List<string>();
            var query = new WorkStepQuery(_workflowRepository);

            if(query.IsLeafStep(workStep))
            {
                classes.Add("workstep");
                classes.Add(GetWorkStepClass(workStep));
            }

            classes.Add(GetLeafDirectory(workStep));

            return classes.Join(' ');
        }

        private static string GetLeafDirectory(WorkStep workStep)
        {
            return workStep.Path.Split('/').Last();
        }

        private static string GetWorkStepClass(WorkStep workStep)
        {
            return string.Format("step-{0}", workStep.WorkItemClass);
        }

        private static string GenerateWorkStepId(WorkStep step)
        {
            return step.Path.Remove(0, 1).Replace('/','.');
        }
    }
}