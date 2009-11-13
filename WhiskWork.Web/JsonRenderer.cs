using System;
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

        public void Render(Stream stream, string path)
        {
            var workStep =_workStepRepository.GetWorkStep(path);
            Render(stream, workStep);
        }

        public void Render(Stream stream, WorkStep workStep)
        {
            using(var writer = new StreamWriter(stream))
            {
                writer.Write("[");

                WriteWorkStepsRecursively(writer, workStep, true);

                writer.Write("]");
            }

       }

        private void WriteWorkStepsRecursively(TextWriter writer, WorkStep workStep, bool first)
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
            WriteWorkStepsRecursively(writer,workStep,true);
        }

        private void RenderNormalStep(TextWriter writer, WorkStep childWorkStep)
        {
            writer.Write("{workstep:");

            writer.Write(CreateWorkStepName(childWorkStep));
            writer.Write(",");

            writer.Write("workitemList:");

            writer.Write("[");
            
            WriteWorkItems(writer, childWorkStep, false);

            writer.Write("]");


            writer.Write("}");

            WriteWorkStepsRecursively(writer, childWorkStep, false);
        }


        private void RenderExpandStep(TextWriter writer, WorkStep workStep)
        {
            writer.Write("{workstep:");

            writer.Write(CreateWorkStepName(workStep));
            writer.Write(",");
            RenderTransientStepsAsWorkItems(writer, workStep);
            writer.Write("}");
        }

        private void RenderTransientStepsAsWorkItems(TextWriter writer, WorkStep step)
        {
            var transientSteps = _workStepRepository.GetChildWorkSteps(step.Path).Where(ws => ws.Type == WorkStepType.Transient);

            writer.Write("workitemList:");

            writer.Write("[");

            bool isFirst = true;

            foreach (var transientStep in transientSteps)
            {
                if(!isFirst)
                {
                    writer.Write(",");
                }
                WriteWorkItems(writer,transientStep,true);

                isFirst = false;
            }

            writer.Write("]");
        }

        private string CreateWorkStepName(WorkStep childWorkStep)
        {
            return "\""+childWorkStep.Path.Replace('/','-').Remove(0,1)+"\"";
        }

        private void WriteWorkItems(TextWriter writer, WorkStep step, bool renderChildWorkSteps)
        {
                //writer.Write(step.WorkItemClass);
                var first = true;

                foreach (var workItem in _workItemRepository.GetWorkItems(step.Path))
                {
                    if (!first)
                    {
                        writer.Write(",");
                    }

                    writer.Write("{");

                    writer.Write("id:\"{0}\"",workItem.Id);

                    RenderProperties(writer, workItem);

                    if(renderChildWorkSteps)
                    {
                        writer.Write(",worksteps:[");

                        //throw new NotImplementedException(_workStepRepository.GetChildWorkSteps(workItem.Path).Count().ToString());

                        var currentStep = _workStepRepository.GetWorkStep(workItem.Path);

                        WriteWorkStepsRecursively(writer,currentStep,true);

                        writer.Write("]");
                        
                    }

                    writer.Write("}");

                    first = false;
                } 
                
        }

        private void RenderProperties(TextWriter writer, WorkItem item)
        {
            foreach (var keyValue in item.Properties)
            {
                writer.Write(",{0}:\"{1}\"",keyValue.Key, keyValue.Value);
            }
        }
    }
}