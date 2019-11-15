using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using RestSharp;
using RestSharp.Authenticators;

namespace BcfAutomation
{
    public static class GetJiraIssuesByProject
    {
        [FunctionName("GetJiraIssuesByProject")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string jiraUrl = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "jiraUrl", true) == 0).Value;
            string projectKey = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "project", true) == 0).Value;

            if (jiraUrl == null || projectKey == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                jiraUrl = data?.jiraUrl;
                projectKey = data?.project;
            }

            if (string.IsNullOrEmpty(jiraUrl) || string.IsNullOrEmpty(projectKey))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a Jira URL (?jiraUrl=) and a project key (or project name) (?project=) on the query string or in the request body.");
            }

            log.Info($"Requested Project Name: {projectKey}");

            List<Issue> allIssues = new List<Issue>();

            try
            {
                RestClient restClient = new RestClient(jiraUrl + "/rest/api/3");
                restClient.CookieContainer = new CookieContainer();
                restClient.Authenticator = new HttpBasicAuthenticator(Environment.GetEnvironmentVariable("JIRA_SERVICE_ACCOUNT_USERNAME"), Environment.GetEnvironmentVariable("JIRA_SERVICE_ACCOUNT_API_KEY"));

                int startAt = 0;
                int total = 0;                
                do
                {
                    string path = $"search?startAt={startAt}&maxResults=100&jql=project={projectKey}";
                    log.Info($"Request Path: {path}");
                    var request1 = new RestRequest(path, Method.GET);
                    request1.AddHeader("Content-Type", "application/json");
                    request1.RequestFormat = RestSharp.DataFormat.Json;
                    var response1 = restClient.Execute(request1);

                    if (response1.StatusCode == HttpStatusCode.OK)
                    {
                        IssueSearchResult result = SimpleJson.DeserializeObject<IssueSearchResult>(response1.Content);
                        allIssues.AddRange(result.issues);
                        total = result.total;
                        startAt += result.issues.Length;
                        log.Info($"total: {total}");
                        log.Info($"startAt: {startAt}");
                    }
                    else
                    {
                        return req.CreateResponse(HttpStatusCode.InternalServerError, response1);
                    }
                    
                } while (startAt <= total);
            }
            catch (Exception ex)
            {
                log.Error($"Unexpected exception: ", ex);
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }

            return req.CreateResponse(HttpStatusCode.OK, allIssues);
        }
    }


    public class IssueSearchResult
    {
        public string expand { get; set; }
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
        public Issue[] issues { get; set; }
    }

    public class Issue
    {
        public string expand { get; set; }
        public string id { get; set; }
        public string self { get; set; }
        public string key { get; set; }
        public dynamic fields { get; set; }
    }
}
