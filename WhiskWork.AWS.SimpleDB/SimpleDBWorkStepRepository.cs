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
    public class SimpleDBWorkStepRepository : IWorkStepRepository
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

        public void CreateWorkStep(WorkStep workStep)
        {
            var putAttributeRequest = new PutAttributesRequest {DomainName = _domain, ItemName = workStep.Path};
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute {Name = "ParentPath", Value = workStep.ParentPath});
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "Type", Value = workStep.Type.ToString()});
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "Ordinal", Value = workStep.Ordinal.ToString() });
            putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "WorkItemClass", Value = workStep.WorkItemClass});

            if(!string.IsNullOrEmpty(workStep.Title))
            {
                putAttributeRequest.Attribute.Add(new ReplaceableAttribute { Name = "Title", Value = workStep.Title });
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
            string parentPath = null;
            var type=WorkStepType.Normal;
            var ordinal=0;
            string workItemClass=null;
            string title=null;

            foreach (var attribute in attributes)
            {
                switch(attribute.Name)
                {
                    case "ParentPath":
                        parentPath = attribute.Value;
                        break;
                    case "Type":
                        type = (WorkStepType)Enum.Parse(typeof(WorkStepType), attribute.Value);
                        break;
                    case "Ordinal":
                        ordinal = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "WorkItemClass":
                        workItemClass = attribute.Value;
                        break;
                    case "Title":
                        title = attribute.Value;
                        break;

                }
            }

            return new WorkStep(path,parentPath,ordinal,type,workItemClass,title);
        }

        public void DeleteWorkStep(string path)
        {
            var deleteAttributesRequest = new DeleteAttributesRequest { ItemName = path, DomainName = _domain};
            _client.DeleteAttributes(deleteAttributesRequest);
        }

        public bool ExistsWorkStep(string path)
        {
            var getAttributesRequest = new GetAttributesRequest { ItemName = path, DomainName = _domain };
            var getAttributesResponse = _client.GetAttributes(getAttributesRequest);

            return getAttributesResponse.GetAttributesResult.Attribute.Count > 0;
        }
    }
}
