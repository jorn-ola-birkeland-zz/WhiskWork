using System;
using System.IO;
using System.Linq;
using System.Xml;
using WhiskWork.Core;
using WhiskWork.Generic;

namespace WhiskWork.Web
{
    public class XmlRenderer : IWorkStepRenderer
    {
        private readonly IWorkStepRepository _workStepRepository;
        private readonly IWorkItemRepository _workItemRepository;

        public XmlRenderer(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository)
        {
            _workStepRepository = workStepRepository;
            _workItemRepository = workItemRepository;
        }

        public string ContentType
        {
            get { return "text/html"; }
        }

        public void Render(Stream stream, string path)
        {
            if (string.IsNullOrEmpty(path) || WorkStep.Root.Path == path)
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
            using (var writer = XmlWriter.Create(stream))
            {
                if(writer==null)
                {
                    throw new ArgumentException("Couldn't create XmlWriter");
                }

                writer.WriteStartDocument();
                
                RenderWorkSteps(writer, workStep);

                writer.WriteEndDocument();
            }
        }

        private void RenderWorkSteps(XmlWriter writer, WorkStep workStep)
        {
            writer.WriteStartElement("WorkSteps");

            foreach (var childWorkStep in _workStepRepository.GetChildWorkSteps(workStep.Path).OrderBy(ws=>ws.Ordinal))
            {
                RenderWorkStep(writer, childWorkStep);
            }


            writer.WriteEndElement(); //WorkSteps

        }

        private void RenderWorkStep(XmlWriter writer, WorkStep workStep)
        {
            writer.WriteStartElement("WorkStep");

            writer.WriteStartAttribute("id");
            writer.WriteValue(GenerateWorkStepId(workStep));
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("workItemClass");
            writer.WriteValue(workStep.WorkItemClass);
            writer.WriteEndAttribute();
            
            RenderWorkSteps(writer, workStep);
            RenderWorkItems(writer, workStep);

            writer.WriteEndElement(); //WorkSteps
        }

        private void RenderWorkItems(XmlWriter writer, WorkStep workStep)
        {
            writer.WriteStartElement("WorkItems");

            foreach (var workItem in _workItemRepository.GetWorkItems(workStep.Path).OrderBy(wi => wi.Ordinal))
            {
                RenderWorkItem(writer, workItem);
            }


            writer.WriteEndElement(); //WorkItems
        }

        private static void RenderWorkItem(XmlWriter writer, WorkItem item)
        {
            writer.WriteStartElement("WorkItem");

            writer.WriteStartAttribute("id");
            writer.WriteValue(item.Id);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("ordinal");
            writer.WriteValue(item.Ordinal);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("classes");
            writer.WriteValue(item.Classes.Join(' '));
            writer.WriteEndAttribute();


            RenderProperties(writer, item);

            writer.WriteEndElement(); //WorkItem
        }

        private static void RenderProperties(XmlWriter writer, WorkItem workItem)
        {
            writer.WriteStartElement("Properties");

            foreach (var keyValue in workItem.Properties)
            {
                writer.WriteStartElement("Property");

                writer.WriteStartAttribute("name");
                writer.WriteValue(keyValue.Key);
                writer.WriteEndAttribute();
                
                writer.WriteValue(keyValue.Value);

                writer.WriteEndElement(); //Property
            }


            writer.WriteEndElement(); //Properties
        }

        private static string GenerateWorkStepId(WorkStep step)
        {
            return step.Path.Remove(0, 1).Replace('/', '.');
        }

    }
}
