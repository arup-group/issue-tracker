using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ARUP.IssueTracker.Classes;
using ARUP.IssueTracker.Classes.BCF2;
using Ionic.Zip;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace BcfAutomation
{
    public static class Function1
    {
        [FunctionName("Function1")]
        [return: Queue("notification")]
        public static OutgoingQueueMessage Run(
            [QueueTrigger("bcf")]IncomingQueueMessage myQueueItem,
            [Blob("bcf/{blobName}", FileAccess.Read)] Stream myBlob,
            TraceWriter log)
        {             
            log.Info($"Jira Address: {myQueueItem.jiraAddress}");
            log.Info($"Jira Project Key: {myQueueItem.jiraProjectKey}");
            log.Info($"Created By: {myQueueItem.createdByEmail}");
            log.Info($"BCF File Name: {myQueueItem.bcfFileName}");
            log.Info($"Blob Name: {myQueueItem.blobName}");
            log.Info($"Blob Size: {myBlob.Length}");

            OutgoingQueueMessage outputMessage = new OutgoingQueueMessage() {
                jiraAddress = myQueueItem.jiraAddress,
                jiraProjectKey = myQueueItem.jiraProjectKey,
                createdByEmail = myQueueItem.createdByEmail,
                bcfFileName = myQueueItem.bcfFileName,
                blobName = myQueueItem.blobName,
                fileSize = myBlob.Length
            };

            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                using (ZipFile zip = ZipFile.Read(myBlob))
                {
                    zip.ExtractAll(tempFolder);
                }

                var result = BcfAdapter.GetBcfIssuesFromPath(tempFolder);
                var issues = result.Item1;
                int errorCount = result.Item2;

                // TODO: clean up viewpoints

                log.Info($"Number of Issues Found: {issues.Count}");
                outputMessage.numberOfIssuesFound = issues.Count;
                log.Info($"Number of Issues Skipped: {errorCount}");
                outputMessage.numberOfIssuesSkipped = errorCount;

                RestClient restClient = new RestClient(myQueueItem.jiraAddress + "/rest/api/2");
                restClient.CookieContainer = new CookieContainer();
                restClient.Authenticator = new HttpBasicAuthenticator(Environment.GetEnvironmentVariable("JIRA_SERVICE_ACCOUNT_USERNAME"), Environment.GetEnvironmentVariable("JIRA_SERVICE_ACCOUNT_API_KEY"));

                List<string> newIssueKeys = new List<string>();
                List<string> updatedIssueKeys = new List<string>();
                List<string> unchangedIssueKeys = new List<string>();

                // look for custom GUID field id and issue type id
                var request6 = new RestRequest("issue/createmeta?expand=projects.issuetypes.fields&projectKeys=" + myQueueItem.jiraProjectKey, Method.GET);
                request6.AddHeader("Content-Type", "application/json");
                request6.RequestFormat = RestSharp.DataFormat.Json;
                var response6 = restClient.Execute(request6);
                if (!CheckResponse(response6, log))
                {                
                    outputMessage.errorMessage = $"Failed to get create issue metadata: {response6.StatusCode.ToString()}";
                    log.Info(outputMessage.errorMessage);
                    return outputMessage;
                }
                string customGuidFieldName = string.Empty;
                string issueTypeId = string.Empty;
                string issueTypeName = string.Empty;
                bool projectFound = false;
                bool guidFound = false;
                var allProjects = JObject.Parse(response6.Content);
                JArray projects = (JArray)allProjects["projects"];
                foreach (var project in projects)
                {
                    if ((string)project["key"] == myQueueItem.jiraProjectKey)
                    {
                        projectFound = true;
                        var allIssueTypes = ((JArray)project["issuetypes"]);
                        foreach (var issueType in allIssueTypes)
                        {
                            foreach (var field in ((JObject)issueType["fields"]).Properties())
                            {
                                if ((string)field.Value["name"] == "GUID")
                                {
                                    customGuidFieldName = field.Name;
                                    issueTypeId = issueType["id"].Value<string>();
                                    issueTypeName = issueType["name"].Value<string>();
                                    guidFound = true;
                                    break;
                                }
                            }
                            if (guidFound)
                            {
                                break;
                            }
                        }
                    }
                }
                if (!projectFound)
                {
                    outputMessage.errorMessage = $"Project not found.";
                    log.Info(outputMessage.errorMessage);
                    return outputMessage;
                }
                if (!guidFound || string.IsNullOrWhiteSpace(customGuidFieldName))
                {
                    outputMessage.errorMessage = $"Failed to find custom GUID field.";
                    log.Info(outputMessage.errorMessage);
                    return outputMessage;
                }
                outputMessage.customGuidFieldName = customGuidFieldName;
                log.Info($"Custom GUID Field Name: {customGuidFieldName}");
                if (string.IsNullOrWhiteSpace(issueTypeId))
                {
                    outputMessage.errorMessage = $"Failed to find issue type id.";
                    log.Info(outputMessage.errorMessage);
                    return outputMessage;
                }
                outputMessage.issueTypeId = issueTypeId;
                log.Info($"Issue Type ID: {issueTypeId}");
                outputMessage.issueTypeName = issueTypeName;
                log.Info($"Issue Type Name: {issueTypeName}");            

                foreach (var issueBcf in issues)
                {
                    var issueJira = new Issue();
                    issueJira.fields = new Fields();
                    //issueJira.fields.creator = new User() { name = jira.Self.name }; // FIXME

                    // add labels if present
                    if (issueBcf.Topic.Labels != null)
                    {
                        issueJira.fields.labels = issueBcf.Topic.Labels.ToList();
                    }

                    // handle and add description
                    //Add annotations for snapshot/viewpoint
                    StringBuilder descriptionBody = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(issueBcf.Topic.Description))
                        descriptionBody.AppendLine(issueBcf.Topic.Description);
                    descriptionBody.AppendLine(string.Format("{{anchor:<Viewpoint>[^{0}]</Viewpoint>}}", "viewpoint.bcfv"));
                    descriptionBody.AppendLine(string.Format("!{0}|thumbnail!", "snapshot.png"));
                    descriptionBody.AppendLine(string.Format("{{anchor:<Snapshot>[^{0}]</Snapshot>}}", "snapshot.png"));
                    issueJira.fields.description = descriptionBody.ToString();

                    // handle comments
                    foreach (var bcfComment in issueBcf.Comment)
                    {
                        if (bcfComment.Viewpoint != null)
                        {
                            ViewPoint bcfViewpoint = issueBcf.Viewpoints.ToList().Find(vp => vp.Guid == bcfComment.Viewpoint.Guid);
                            //Add annotations for snapshot/viewpoint
                            StringBuilder commentBody = new StringBuilder();
                            commentBody.AppendLine(bcfComment.Comment1);
                            if (bcfViewpoint != null)
                            {
                                if (!string.IsNullOrWhiteSpace(bcfViewpoint.Viewpoint))
                                {
                                    commentBody.AppendLine(string.Format("{{anchor:<Viewpoint>[^{0}]</Viewpoint>}}", bcfViewpoint.Viewpoint));
                                }
                                if (!string.IsNullOrWhiteSpace(bcfViewpoint.Snapshot))
                                {
                                    commentBody.AppendLine(string.Format("!{0}|thumbnail!", bcfViewpoint.Snapshot));
                                    commentBody.AppendLine(string.Format("{{anchor:<Snapshot>[^{0}]</Snapshot>}}", bcfViewpoint.Snapshot));
                                }
                            }

                            bcfComment.Comment1 = commentBody.ToString();
                        }
                    }

                    // upload to Jira
                    try
                    {
                        //CHECK IF ALREADY EXISTING
                        // could use the expression: cf[11600] ~ "aaaa"
                        // = operator not supported
                        string fields = " AND  GUID~" + issueBcf.Topic.Guid + "&fields=key,comment";
                        string query = "search?jql=project=\"" + myQueueItem.jiraProjectKey + "\"" + fields;

                        var request4 = new RestRequest("search", Method.GET);                    
                        request4.AddQueryParameter("jql", "project=\"" + myQueueItem.jiraProjectKey + "\"" + " AND  GUID~" + issueBcf.Topic.Guid);
                        request4.AddQueryParameter("fields", "key,comment");
                        request4.AddHeader("Content-Type", "application/json");
                        request4.RequestFormat = RestSharp.DataFormat.Json;
                        var response4 = restClient.Execute<Issues>(request4);

                        if (!CheckResponse(response4, log))
                        {
                            log.Info($"Failed to check issue existence: {response4.StatusCode.ToString()}");
                            break;
                        }

                        //DOESN'T exist already
                        if (!response4.Data.issues.Any())
                        {
                            //files to be uploaded
                            List<string> filesToBeUploaded = new List<string>();
                            if (File.Exists(Path.Combine(tempFolder, issueBcf.Topic.Guid, "markup.bcf")))
                                filesToBeUploaded.Add(Path.Combine(tempFolder, issueBcf.Topic.Guid, "markup.bcf"));
                            issueBcf.Viewpoints.ToList().ForEach(vp => {
                                if (!string.IsNullOrWhiteSpace(vp.Snapshot) && File.Exists(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Snapshot)))
                                    filesToBeUploaded.Add(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Snapshot));
                                if (!string.IsNullOrWhiteSpace(vp.Viewpoint) && File.Exists(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Viewpoint)))
                                    filesToBeUploaded.Add(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Viewpoint));
                            });
                            string key = "";

                            var request = new RestRequest("issue", Method.POST);
                            request.AddHeader("Content-Type", "application/json");
                            request.RequestFormat = RestSharp.DataFormat.Json;

                            var newissue =
                                new
                                {

                                    fields = new Dictionary<string, object>()

                                };
                            newissue.fields.Add("project", new { key = myQueueItem.jiraProjectKey });
                            if (!string.IsNullOrWhiteSpace(issueJira.fields.description))
                            {
                                newissue.fields.Add("description", issueJira.fields.description);
                            }
                            newissue.fields.Add("summary", (string.IsNullOrWhiteSpace(issueBcf.Topic.Title)) ? "no title" : issueBcf.Topic.Title);
                            newissue.fields.Add("issuetype", new { id = issueTypeId });
                            newissue.fields.Add(customGuidFieldName, issueBcf.Topic.Guid);

                            if (issueJira.fields.labels != null && issueJira.fields.labels.Any())
                                newissue.fields.Add("labels", issueJira.fields.labels);

                            request.AddBody(newissue);
                            var response = restClient.Execute(request);

                            var responseIssue = new Issue();
                            if (CheckResponse(response, log))
                            {
                                responseIssue = RestSharp.SimpleJson.DeserializeObject<Issue>(response.Content);
                                key = responseIssue.key; //attach and comment sent to the new issue
                                newIssueKeys.Add(key);
                                log.Info($"Issue created: {key}");                            
                            }
                            else
                            {
                                log.Info(response.Content);
                                log.Info($"Failed to create issue: {response.StatusCode.ToString()}");
                                break;
                            }

                            //upload all viewpoints and snapshots
                            var request2 = new RestRequest("issue/" + key + "/attachments", Method.POST);
                            request2.AddHeader("X-Atlassian-Token", "nocheck");
                            request2.RequestFormat = RestSharp.DataFormat.Json;
                            filesToBeUploaded.ForEach(file => request2.AddFile("file", File.ReadAllBytes(file), Path.GetFileName(file)));
                            var response2 = restClient.Execute(request2);
                            if (!CheckResponse(response2, log))
                            {
                                log.Info($"Failed to create upload attachments: {response2.StatusCode.ToString()}");
                            }

                            //ADD COMMENTS
                            if (issueBcf.Comment.Any())
                            {
                                foreach (var c in issueBcf.Comment)
                                {
                                    if (string.IsNullOrWhiteSpace(c.Comment1))
                                    {
                                        continue;
                                    }
                                    var request3 = new RestRequest("issue/" + key + "/comment", Method.POST);
                                    request3.AddHeader("Content-Type", "application/json");
                                    request3.RequestFormat = RestSharp.DataFormat.Json;
                                    var newcomment = new { body = c.Comment1 };
                                    request3.AddBody(newcomment);
                                    var response3 = restClient.Execute<Comment2>(request3);
                                    if (!CheckResponse(response3, log))
                                    {
                                        log.Info($"Failed to add comment: {response3.StatusCode.ToString()}");
                                        break;
                                    }
                                }
                            }
                        }
                        else //UPDATE ISSUE
                        {
                            var oldIssue = response4.Data.issues.First();
                            if (issueBcf.Comment.Any())
                            {
                                int unmodifiedCommentNumber = 0;
                                foreach (var c in issueBcf.Comment)
                                {
                                    //clean all metadata annotations
                                    string newComment = c.Comment1;
                                    string normalized1 = Regex.Replace(newComment, @"\s", "");
                                    if (string.IsNullOrWhiteSpace(c.Comment1) || oldIssue.fields.comment.comments.Any(o => Regex.Replace(o.body, @"\s", "").Equals(normalized1, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        unmodifiedCommentNumber++;
                                        continue;
                                    }

                                    var request3 = new RestRequest("issue/" + oldIssue.key + "/comment", Method.POST);
                                    request3.AddHeader("Content-Type", "application/json");
                                    request3.RequestFormat = RestSharp.DataFormat.Json;
                                    var newcomment = new { body = c.Comment1 };
                                    request3.AddBody(newcomment);
                                    var response3 = restClient.Execute<Comment2>(request3);

                                    //upload viewpoint and snapshot
                                    var request5 = new RestRequest("issue/" + oldIssue.key + "/attachments", Method.POST);
                                    request5.AddHeader("X-Atlassian-Token", "nocheck");
                                    request5.RequestFormat = RestSharp.DataFormat.Json;
                                    issueBcf.Viewpoints.ToList().ForEach(vp => {
                                        if (c.Viewpoint != null)
                                        {
                                            if (vp.Guid == c.Viewpoint.Guid)
                                            {
                                                if (File.Exists(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Snapshot)))
                                                    request5.AddFile("file", File.ReadAllBytes(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Snapshot)), vp.Snapshot);
                                                if (File.Exists(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Viewpoint)))
                                                    request5.AddFile("file", File.ReadAllBytes(Path.Combine(tempFolder, issueBcf.Topic.Guid, vp.Viewpoint)), vp.Viewpoint);
                                            }
                                        }
                                    });
                                    if (request5.Files.Count > 0)
                                    {
                                        var response5 = restClient.Execute(request5);
                                        CheckResponse(response5, log);
                                    }

                                    if (!CheckResponse(response3, log))
                                    {
                                        break;
                                    }
                                }

                                if (unmodifiedCommentNumber == issueBcf.Comment.Count)
                                {
                                    unchangedIssueKeys.Add(oldIssue.key);
                                    log.Info($"Issue Unchanged: {oldIssue.key}");
                                }
                                else
                                {
                                    updatedIssueKeys.Add(oldIssue.key);
                                    log.Info($"Issue Updated: {oldIssue.key}");
                                }
                            }
                            else
                            {
                                unchangedIssueKeys.Add(oldIssue.key);
                                log.Info($"Issue Unchanged: {oldIssue.key}");                            
                            }
                        }

                    } // END TRY
                    catch (System.Exception ex)
                    {
                        log.Error($"Exception happened when uploading to Jira: {tempFolder}", ex);
                    }                
                }

                outputMessage.issuesCreated = newIssueKeys;
                log.Info($"Number of Issues Created: {newIssueKeys.Count}");
                outputMessage.issuesUpdated = updatedIssueKeys;
                log.Info($"Number of Issues Updated: {updatedIssueKeys.Count}");
                outputMessage.issuesUnchanged= unchangedIssueKeys;
                log.Info($"Number of Issues Unchanged: {unchangedIssueKeys.Count}");

                try
                {
                    DeleteDirectory(tempFolder);
                }
                catch(Exception ex)
                {
                    log.Error($"Failed to delete temp folder: {tempFolder}", ex);
                }
            }
            catch(Exception ex)
            {
                log.Error($"Unexpected exception: ", ex);
                outputMessage.errorMessage = ex.ToString();
                return outputMessage;
            }

            outputMessage.isSuccessful = true;
            return outputMessage;
        }

        private static bool CheckResponse(IRestResponse response, TraceWriter log)
        {
            bool isOK = response != null && (response.StatusCode == System.Net.HttpStatusCode.OK
                    || response.StatusCode == System.Net.HttpStatusCode.Created
                    || response.StatusCode == System.Net.HttpStatusCode.NoContent);

            if (!isOK)
            {
                log.Info($"Request Failed: {response.ResponseUri}");
                log.Info($"Status Code: {response.StatusCode}");
                log.Info($"Response Body: {response.Content}");
            }

            return isOK;
        }

        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        private static void DeleteDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
    }

    public class IncomingQueueMessage
    {
        public string jiraAddress { get; set; }
        public string jiraProjectKey { get; set; }
        public string blobName { get; set; }
        public string bcfFileName { get; set; }
        public string createdByEmail { get; set; }
    }

    public class OutgoingQueueMessage
    {
        public string jiraAddress { get; set; }
        public string jiraProjectKey { get; set; }
        public string createdByEmail { get; set; }
        public bool isSuccessful { get; set; }
        public string errorMessage { get; set; }
        public string bcfFileName { get; set; }
        public string blobName { get; set; }
        public long fileSize { get; set; }
        public int numberOfIssuesFound { get; set; }
        public int numberOfIssuesSkipped { get; set; }
        public string customGuidFieldName { get; set; }
        public string issueTypeId { get; set; }
        public string issueTypeName { get; set; }
        public List<string> issuesCreated { get; set; }
        public List<string> issuesUpdated { get; set; }
        public List<string> issuesUnchanged { get; set; }
    }
}
