using System;
using System.Collections.Generic;
using System.IO;
using WhiskWork.Core;
using System.Linq;

namespace WhiskWork.Web
{
    public class JsonRenderer : IWorkStepRenderer
    {
        private readonly IWorkStepRepository _workStepRepository;
        private readonly IWorkItemRepository _workItemRepository;

        public JsonRenderer(IWorkStepRepository workStepRepository, IWorkItemRepository workItemRepository)
        {
            _workStepRepository = workStepRepository;
            _workItemRepository = workItemRepository;
        }

        public string ContentType
        {
            get { return "application/json"; }
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
            using(var writer = new StreamWriter(stream))
            {
                writer.Write("[");

                RenderWorkStepsRecursively(writer, workStep, true);

                writer.Write("]");
            }

       }

        private void RenderWorkStepsRecursively(TextWriter writer, WorkStep workStep, bool first)
        {
            foreach (var childWorkStep in _workStepRepository.GetChildWorkSteps(workStep.Path))
            {
                if(!first)
                {
                    writer.Write(",");
                }

                if (_workStepRepository.IsExpandStep(childWorkStep))
                {
                    RenderExpandStep(writer, childWorkStep);
                }
                else if (_workStepRepository.IsParallelStep(childWorkStep))
                {
                    RenderParallelStep(writer, childWorkStep);
                }
                else
                {
                    RenderNormalStep(writer, childWorkStep);
                }

                first = false;
            } 
        }

        private void RenderParallelStep(TextWriter writer, WorkStep workStep)
        {
            RenderWorkStepsRecursively(writer,workStep,true);
        }

        private void RenderNormalStep(TextWriter writer, WorkStep childWorkStep)
        {
            RenderWorkStep(writer, childWorkStep);

            RenderWorkStepsRecursively(writer, childWorkStep, false);
        }

        private void RenderExpandStep(TextWriter writer, WorkStep workStep)
        {
            RenderWorkStep(writer, workStep);
        }

        private void RenderWorkStep(TextWriter writer, WorkStep childWorkStep)
        {
            writer.Write("{workstep:");

            writer.Write(CreateWorkStepName(childWorkStep));
            writer.Write(",");

            writer.Write("workitemList:");

            writer.Write("[");

            RenderWorkItems(writer, childWorkStep);

            writer.Write("]");


            writer.Write("}");
        }

        private void RenderWorkItems(TextWriter writer, WorkStep step)
        {
                var first = true;

                foreach (var workItem in _workItemRepository.GetWorkItems(step.Path).OrderBy(wi=>wi.Ordinal))
                {
                    if (!first)
                    {
                        writer.Write(",");
                    }

                    RenderWorkItem(step, writer, workItem);

                    first = false;
                }
        }

        private void RenderWorkItem(WorkStep step, TextWriter writer, WorkItem workItem)
        {
            writer.Write("{");

            writer.Write("id:\"{0}\"",workItem.Id);

            RenderProperties(writer, workItem);

            RenderTransientWorkSteps(step, writer, workItem);

            writer.Write("}");
        }


        private static void RenderProperties(TextWriter writer, WorkItem item)
        {
            foreach (var keyValue in item.Properties)
            {
                writer.Write(",{0}:\"{1}\"",keyValue.Key, keyValue.Value);
            }
        }

        private void RenderTransientWorkSteps(WorkStep step, TextWriter writer, WorkItem workItem)
        {
            var childStepPath = ExpandedWorkStep.GetTransientPath(step, workItem);

            if (_workStepRepository.ExistsWorkStep(childStepPath))
            {
                var childStep = _workStepRepository.GetWorkStep(childStepPath);
                writer.Write(",worksteps:[");

                RenderWorkStepsRecursively(writer, childStep, true);

                writer.Write("]");

            }
        }

        private static string CreateWorkStepName(WorkStep childWorkStep)
        {
            return "\"" + childWorkStep.Path.Replace('/', '-').Remove(0, 1) + "\"";
        }


    }
}