using System;
using System.Collections.Generic;
using System.Xml;
using Amazon;
using Amazon.SimpleDB;
using WhiskWork.Core;
using Amazon.SimpleDB.Model;
using Attribute=Amazon.SimpleDB.Model.Attribute;

namespace WhiskWork.AWS.SimpleDB
{
    public class SimpleDBWorkStepRepository : ICacheableWorkStepRepository
    {
        private readonly AmazonSimpleDB _client;
        private readonly string _domain;
        public SimpleDBWorkStepRepository(string domain, string accessKey, string secretKey)
        {
            _domain = domain;
            _client = AWSClientFactory.CreateAmazonSimpleDBClient(accessKey, secretKey);
            EnsureDomain(_domain);
        }

        private void EnsureDomain(string domain)
        {
            var listDomainsRequest = new ListDomainsRequest();
            var listDomainsResponse = _client.ListDomains(listDomainsRequest);

            if (listDomainsResponse.ListDomainsResult.DomainName.Contains(domain))
            {
                return;
            }

            var createDomainRequest = new CreateDomainRequest {DomainName = domain};
            _client.CreateDomain(createDomainRequest);
        }

        public IEnumerable<WorkStep> GetAllWorkSteps()
        {
            var selectRequest =
                new SelectRequest
                {
                    SelectExpression = string.Format("select * from {0}", _domain)
                };

            var selectResponse = _client.Select(selectRequest);

            foreach (var item in selectResponse.SelectResult.Item)
            {
                yield return GenerateWorkStep(item.Name, item.Attribute);
            }
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            SendUpdateRequest(workStep);
        }

        private void SendUpdateRequest(WorkStep workStep)
        {
            var putAttributeRequest = new PutAttributesRequest {DomainName = _domain, ItemName = workStep.Path};
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "Type", Value = workStep.Type.ToString(), Replace = true });
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "Ordinal", Value = workStep.Ordinal.ToString(), Replace = true });
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "WorkItemClass", Value = workStep.WorkItemClass, Replace = true });

            if(!string.IsNullOrEmpty(workStep.Title))
            {
                putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "Title", Value = workStep.Title, Replace = true });
            }

            _client.PutAttributes(putAttributeRequest);
        }

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            var selectRequest = new SelectRequest();
            selectRequest.SelectExpression = string.Format("select * from {0} where ParentPath='{1}'",_domain,path);
           
            var selectResponse = _client.Select(selectRequest);

            foreach (var item in selectResponse.SelectResult.Item)
            {
                yield return GenerateWorkStep(item.Name, item.Attribute);
            }
        }

        public WorkStep GetWorkStep(string path)
        {
            var getAttributesRequest = new GetAttributesRequest {ItemName = path, DomainName = _domain};
            var getAttributesResponse = _client.GetAttributes(getAttributesRequest);

            var attributes = getAttributesResponse.GetAttributesResult.Attribute;
            return GenerateWorkStep(path, attributes);
        }

        private static WorkStep GenerateWorkStep(string path, IEnumerable<Attribute> attributes)
        {
            var step = WorkStep.New(path);

            foreach (var attribute in attributes)
            {
                switch(attribute.Name)
                {
                    case "Type":
                        step = step.UpdateType((WorkStepType)Enum.Parse(typeof(WorkStepType), attribute.Value));
                        break;
                    case "Ordinal":
                        step = step.UpdateOrdinal(XmlConvert.ToInt32(attribute.Value));
                        break;
                    case "WorkItemClass":
                        step = step.UpdateWorkItemClass(attribute.Value);
                        break;
                    case "Title":
                        step = step.UpdateTitle(attribute.Value);
                        break;

                }
            }

            return step;
        }

        public void DeleteWorkStep(string path)
        {
            var deleteAttributesRequest = new DeleteAttributesRequest { ItemName = path, DomainName = _domain};
            _client.DeleteAttributes(deleteAttributesRequest);
        }

        public void UpdateWorkStep(WorkStep workStep)
        {
            DeleteWorkStep(workStep.Path);
            SendUpdateRequest(workStep);
        }

        public bool ExistsWorkStep(string path)
        {
            var getAttributesRequest = new GetAttributesRequest { ItemName = path, DomainName = _domain };
            var getAttributesResponse = _client.GetAttributes(getAttributesRequest);

            return getAttributesResponse.GetAttributesResult.Attribute.Count > 0;
        }
    }
}
