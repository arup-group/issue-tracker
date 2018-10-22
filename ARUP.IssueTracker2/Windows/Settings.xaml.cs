using Arup.RestSharp;
using ARUP.IssueTracker.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ARUP.IssueTracker.Windows
{
    // class for checking the GUID field
    class JiraCustomField 
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        public Settings()
        {            
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            List<JiraAccount> accounts = jiraAccounts.ItemsSource as List<JiraAccount>;
            
            // ensure only one active account
            if (accounts.FindAll(ac => ac.active).Count != 1)
            {
                MessageBox.Show("You need to select only one active account!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // check guid field id for active account
            JiraAccount activeAccount = accounts.Find(ac => ac.active);
            if(string.IsNullOrWhiteSpace(activeAccount.username))
            {
                // for setting BCF username only
                DialogResult = true;
                return;
            }
            var client = new RestClient(activeAccount.jiraserver);
            client.CookieContainer = new System.Net.CookieContainer();

            bool isJiraCloud = false;
            if (activeAccount.jiraserver.ToLower().Contains("atlassian.net"))
            {
                isJiraCloud = true;
                client.Authenticator = new HttpBasicAuthenticator(activeAccount.username, activeAccount.password);
            }

            var request = new RestRequest("/rest/auth/1/session", Method.POST);
            request.AddHeader("content-type", "application/json");
            string requestBody = "{\"username\": \"" + activeAccount.username + "\",\"password\": \"" + activeAccount.password + "\"}";
            request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (isJiraCloud || response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var request2 = new RestRequest("/rest/api/2/field", Method.GET);                
                IRestResponse<List<JiraCustomField>> response2 = null;
                try
                {
                    response2 = client.Execute<List<JiraCustomField>>(request2);
                    JiraCustomField guidField = response2.Data.Find(field => field.name == "GUID");
                    if (guidField != null)
                    {
                        activeAccount.guidfield = guidField.id;
                        MessageBox.Show(string.Format("The GUID field Id on {0} is {1}. Now you can create an issue with the plug-ins of Revit and Navisworks!", activeAccount.jiraserver, guidField.id), "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(string.Format("There's no custom field named GUID on {0}. Creating an issue might fail. Please check with your Jira administrator!", activeAccount.jiraserver), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    RestCallback.LogRequest(client, request2, response2, ex);
                    MessageBox.Show(string.Format("Cannot connect to {0}. Please check with your Jira administrator!\nStatus Code: " + response2.StatusCode.ToString(), activeAccount.jiraserver), "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                RestCallback.LogRequest(client, request, response);
                MessageBox.Show("Login failed. Please check your username/password!\nStatus Code: " + response.StatusCode.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }                        

            DialogResult = true;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;

            // Set focus for single click editing
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }
                DataGrid dataGrid = FindParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                        {
                            row.IsSelected = true;
                        }
                    }
                }
            }
        }

        private void PasswordBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = sender as PasswordBox;
            int currentIndex = FindParent<DataGridRow>(pb).GetIndex();
            ((JiraAccount)(jiraAccounts.Items[currentIndex])).password = pb.Password;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private void addJiraCloudAccountButton_Click(object sender, RoutedEventArgs e)
        {
            JiraCloudSetup window = new JiraCloudSetup();
            var result = window.ShowDialog();
            if (result.HasValue && result.Value) 
            {
                List<JiraAccount> accounts = jiraAccounts.ItemsSource as List<JiraAccount>;
                accounts.ForEach(ac => ac.active = false);
                accounts.Add(new JiraAccount() { active = true, jiraserver = window.jiraCloudAddress, username = window.email, password = window.apiToken });
                jiraAccounts.Items.Refresh();
                Button_Click_1(null, null);
            }
        }
    }
}
