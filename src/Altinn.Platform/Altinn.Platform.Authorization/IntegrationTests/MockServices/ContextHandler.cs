using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Interface;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Storage.Interface.Models;
using Authorization.Platform.Authorization.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class ContextHandler : IContextHandler
    {

        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IRoles _rolesWrapper;

        private readonly string OrgAttributeId = "urn:altinn:org";

        private readonly string AppAttributeId = "urn:altinn:app";

        private readonly string InstanceAttributeId = "urn:altinn:instance-id";

        private readonly string TaskAttributeId = "urn:altinn:task";

        private readonly string PartyAttributeId = "urn:altinn:partyid";

        private readonly string UserAttributeId = "urn:altinn:user-id";

        private readonly string AltinnRoleAttributeId = "urn:altinn:rolecode";


        public ContextHandler(IHttpContextAccessor httpContextAccessor, IRoles rolesWrapper)
        {
            _httpContextAccessor = httpContextAccessor;
            _rolesWrapper = rolesWrapper;
        }

        public async Task<XacmlContextRequest> Enrich(XacmlContextRequest request)
        {
            string testID = GetTestId(_httpContextAccessor.HttpContext);
            if (!string.IsNullOrEmpty(testID) && testID.ToLower().Contains("altinnapps"))
            {
                await EnrichResourceAttributes(request);
            }
            else
            {
                try
                {
                    return ParseRequest(testID + "Request_Enriched.xml", GetConformancePath());
                }
                catch (Exception)
                {

                }
            }

            return request;
        }

        private async Task EnrichResourceAttributes(XacmlContextRequest request)
        {
            string orgAttributeValue = string.Empty;
            string appAttributeValue = string.Empty;
            string instanceAttributeValue = string.Empty;
            string resourcePartyAttributeValue = string.Empty;
            string taskAttributeValue = string.Empty;

            XacmlContextAttributes resourceContextAttributes = request.GetResourceAttributes();
      
            foreach (XacmlAttribute attribute in resourceContextAttributes.Attributes)
            {
                if (attribute.AttributeId.OriginalString.Equals(OrgAttributeId))
                {
                    orgAttributeValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(AppAttributeId))
                {
                    appAttributeValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(InstanceAttributeId))
                {
                    instanceAttributeValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(TaskAttributeId))
                {
                    taskAttributeValue = attribute.AttributeValues.First().Value;
                }

                if (attribute.AttributeId.OriginalString.Equals(PartyAttributeId))
                {
                    resourcePartyAttributeValue = attribute.AttributeValues.First().Value;
                }
            }


            bool resourceAttributeComplete = false;

            if (!string.IsNullOrEmpty(orgAttributeValue) &&
                !string.IsNullOrEmpty(appAttributeValue) &&
                !string.IsNullOrEmpty(instanceAttributeValue) &&
                !string.IsNullOrEmpty(resourcePartyAttributeValue) &&
                !string.IsNullOrEmpty(taskAttributeValue))
            {
                // The resource attributes are complete
                resourceAttributeComplete = true;
            }
            else if (!string.IsNullOrEmpty(orgAttributeValue) &&
                !string.IsNullOrEmpty(appAttributeValue) &&
                string.IsNullOrEmpty(instanceAttributeValue) &&
                !string.IsNullOrEmpty(resourcePartyAttributeValue) &&
                string.IsNullOrEmpty(taskAttributeValue))
                {
                // The resource attributes are complete
                resourceAttributeComplete = true;
            }

            if (!resourceAttributeComplete)
            {
                Instance instanceData = GetTestInstance(instanceAttributeValue);

                if (string.IsNullOrEmpty(orgAttributeValue))
                {
                    resourceContextAttributes.Attributes.Add(GetOrgAttribute(instanceData));
                }

                if (string.IsNullOrEmpty(appAttributeValue))
                {
                    resourceContextAttributes.Attributes.Add(GetAppAttribute(instanceData));
                }

                if (string.IsNullOrEmpty(taskAttributeValue))
                {
                    resourceContextAttributes.Attributes.Add(GetProcessElementAttribute(instanceData));
                }

                if (string.IsNullOrEmpty(resourcePartyAttributeValue))
                {
                    resourceContextAttributes.Attributes.Add(GetPartyAttribute(instanceData));
                }

                resourcePartyAttributeValue = instanceData.InstanceOwner.PartyId;
            }

            await EnrichSubjectAttributes(request, resourcePartyAttributeValue);
        }

        private async Task EnrichSubjectAttributes(XacmlContextRequest request, string resourceParty)
        {
            XacmlContextAttributes subjectContextAttributes = request.GetSubjectAttributes();

            int subjectUserId = 0;
            int resourcePartyId = Convert.ToInt32(resourceParty);

            foreach (XacmlAttribute xacmlAttribute in subjectContextAttributes.Attributes)
            {
                if (xacmlAttribute.AttributeId.OriginalString.Equals(UserAttributeId))
                {
                    subjectUserId = Convert.ToInt32(xacmlAttribute.AttributeValues.First().Value);
                }
            }

            if (subjectUserId == 0)
            {
                return; 
            }

            List<Role> roleList = await _rolesWrapper.GetDecisionPointRolesForUser(subjectUserId, resourcePartyId) ?? new List<Role>();

            subjectContextAttributes.Attributes.Add(GetRoleAttribute(roleList));
        }

        private XacmlAttribute GetOrgAttribute(Instance instance)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(OrgAttributeId), false);
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), instance.Org));
            return attribute;
        }

        private XacmlAttribute GetAppAttribute(Instance instance)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(AppAttributeId), false);
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), instance.AppId.Split('/')[1]));
            return attribute;
        }

        private XacmlAttribute GetProcessElementAttribute(Instance instance)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(TaskAttributeId), false);
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), instance.Process.CurrentTask.ElementId));
            return attribute;
        }

        private XacmlAttribute GetPartyAttribute(Instance instance)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(PartyAttributeId), false);
            // When Party attribute is missing from input it is good to return it so PEP can get this information
            attribute.IncludeInResult = true;
            attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), instance.InstanceOwner.PartyId));
            return attribute;
        }

        private XacmlAttribute GetRoleAttribute(List<Role> roles)
        {
            XacmlAttribute attribute = new XacmlAttribute(new Uri(AltinnRoleAttributeId), false);
            foreach (Role role in roles)
            {
                attribute.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), role.Value));
            }

            return attribute;

        }

        private Instance GetInstance(string instanceId)
        {
            return new Instance();


        }

        private string GetTestId(HttpContext context)
        {
           return context.Request.Headers["testcase"];
        }

        private string GetAltinnAppsPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.CodeBase).LocalPath);
            return Path.Combine(unitTestFolder, @"..\..\..\Data\Xacml\3.0\AltinnApps");
        }

        private string GetConformancePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.CodeBase).LocalPath);
            return Path.Combine(unitTestFolder, @"..\..\..\Data\Xacml\3.0\ConformanceTests");
        }

        private XacmlContextRequest ParseRequest(string requestDocumentTitle, string requestPath)
        {
            XmlDocument requestDocument = new XmlDocument();
            requestDocument.Load(Path.Combine(requestPath, requestDocumentTitle));
            XacmlContextRequest contextRequest;
            using (XmlReader reader = XmlReader.Create(new StringReader(requestDocument.OuterXml)))
            {
                contextRequest = XacmlParser.ReadContextRequest(reader);
            }

            return contextRequest;
        }

        private string GetInstancePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.CodeBase).LocalPath);
            return Path.Combine(unitTestFolder, @"..\..\..\Data\Instances");
        }

        private Instance GetTestInstance(string instanceId)
        {
            string content = null;

            string partyPart = instanceId.Split('/')[0];
            string instancePart = instanceId.Split('/')[1];

            content = System.IO.File.ReadAllText(Path.Combine(GetInstancePath(), partyPart + @"\" + instancePart +".json"));
            Instance instance = (Instance)JsonConvert.DeserializeObject(content, typeof(Instance));
            return instance;
        }
    }
}
