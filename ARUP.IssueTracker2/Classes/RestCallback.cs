using Arup.RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace ARUP.IssueTracker.Classes
{
    public static class RestCallback
    {
        public static bool Check(IRestResponse response)
        {
            try
            {
                if (null == response)
                {
                    string error = "Please check your connection.";
                    MessageBox.Show(error, "Unknown error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }



                if (response.StatusCode != System.Net.HttpStatusCode.OK
                    && response.StatusCode != System.Net.HttpStatusCode.Created
                    && response.StatusCode != System.Net.HttpStatusCode.NoContent
                    && response != null)
                {
                    object ve;
                    if (Arup.RestSharp.SimpleJson.TryDeserializeObject(response.Content, out  ve))
                    {
                        ErrorMsg validationErrorResponse = Arup.RestSharp.SimpleJson.DeserializeObject<ErrorMsg>(response.Content);
                        string error = "";
                        if (validationErrorResponse.errorMessages.Any())
                        {
                            foreach (var str in validationErrorResponse.errorMessages)
                            {
                                error += str + "\n";
                            }

                        }
                        error += (null != validationErrorResponse.errors && validationErrorResponse.errors.ToString().Replace(" ", "") != "{}") ? validationErrorResponse.errors.ToString() : "";

                        if (error.Contains("customfield"))  // when there's no custom field on Jira
                        {
                            MessageBox.Show("The custom GUID field is missing from this Jira project. The Issue Tracker requires this field. Please ask your Jira administrators to check that the GUID field is part of the field configuration and visible on the Create/Edit screens for all issue types in this project.", 
                                response.StatusDescription, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else if (error.Contains("Comment body can not be empty"))
                        {
                            // ignore this error because Solibri generates blank BCF comments
                        }
                        else  // other unsuccessful reponses
                        {
                            MessageBox.Show(error, response.StatusDescription, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        
                    }
                    else
                    {
                        string error = response.StatusDescription;
                        if (string.IsNullOrWhiteSpace(error))
                            error = "Please check your connection.";
                        MessageBox.Show(error, response.ResponseStatus.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return false;
                }
                
            }
            catch (Exception ex1)
            {
                MessageBox.Show("exception: " + ex1);
            }

            return true;
        }

        public static void LogRequest(RestClient client, IRestRequest request, IRestResponse response, Exception exception = null)
        {
            var requestToLog = new
            {
                resource = request.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                parameters = request.Parameters.Select(parameter => new
                {
                    name = parameter.Name,
                    value = parameter.Value,
                    type = parameter.Type.ToString()
                }),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                uri = client.BuildUri(request),
            };

            var responseToLog = new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                responseUri = response.ResponseUri,
                errorMessage = response.ErrorMessage,
            };

            long currentTime = (long) DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            File.WriteAllText(Path.Combine(Path.GetTempPath(), string.Format("AIT_Log_{0}.txt", currentTime)),
                string.Format("Request:\n{0}\n\nResponse:\n{1}\n\nException:\n{2}", 
                JsonUtils.Serialize(requestToLog), 
                JsonUtils.Serialize(responseToLog), 
                exception != null ? exception.ToString() : string.Empty));
        }
    }
}
