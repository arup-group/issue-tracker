using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using ARUP.IssueTracker.Classes;
using ARUP.IssueTracker.UserControls;
using Arup.RestSharp;

namespace ARUP.IssueTracker.Windows
{
    /// <summary>
    /// Interaction logic for UploadBCF.xaml
    /// </summary>
    public partial class UploadBCF : Window
    {
        public int itemCount = 0;

        public User selectedAssignee;
        public List<Component> selectedComponents = new List<Component>(); 

        public UploadBCF()
        {
            InitializeComponent();
        }
        public void setValues(){
            string s = (itemCount > 1) ? "s" : "";
            description.Content = "You are about to send "+itemCount.ToString()+" Issue"+s+" to Jira.";
            //projCombo.ItemsSource = ProjectsCollection;
            //projCombo.SelectedIndex = projIndex;
        }
        private void OKBtnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void CancelBtnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void projCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (issueTypeCombo.Items.Count != 0)
                issueTypeCombo.SelectedIndex = 0;
        }

        private void ChangeAssign_OnClick(object sender, RoutedEventArgs e)
        {
            if (isAnyProjectSelected())
            {
                return;
            } 

            List<User> assignees = getAssigneesProj();
            if (!assignees.Any())
            {
                MessageBox.Show("You don't have permission to Assign people to this Issue");
                return;
                //jira.issuesCollection[jiraPan.issueList.SelectedIndex].transitions = response2.Data.transitions;
            }
            ChangeAssignee cv = new ChangeAssignee(); cv.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            cv.SetList(assignees);

            cv.Title = "Assign to";
            cv.ShowDialog();
            if (cv.DialogResult.HasValue && cv.DialogResult.Value)
            {
                selectedAssignee = (cv.valuesList.SelectedIndex >= cv.valuesList.Items.Count || cv.valuesList.SelectedIndex == -1) ? null : (User)cv.valuesList.SelectedItem;
                ChangeAssign.Content = (selectedAssignee != null) ? selectedAssignee.displayName : "none";
            }
        }

        private void ChangeComponents_OnClick(object sender, RoutedEventArgs e)
        {
            if (isAnyProjectSelected())
            {
                return;
            } 

            try
            {
                ChangeValue cv = new ChangeValue();
                cv.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                List<Component> components = getComponents();
                cv.valuesList.ItemsSource = components;
                cv.valuesList.SelectionMode = SelectionMode.Multiple;

                cv.Title = "Change Components";
                DataTemplate componentTemplate = cv.FindResource("componentTemplate") as DataTemplate;
                cv.valuesList.ItemTemplate = componentTemplate;
                // ChangeStatus ChangSt = new ChangeStatus(jira.issuesCollection[jiraPan.issueList.SelectedIndex].transitions);
                cv.ShowDialog();
                if (cv.DialogResult.HasValue && cv.DialogResult.Value)
                {
                    selectedComponents = new List<Component>();

                    foreach (var c in cv.valuesList.SelectedItems)
                    {
                        Component cc = c as Component;
                        selectedComponents.Add(cc);

                    }

                    string componentsout = "none";

                    if (selectedComponents != null && selectedComponents.Any())
                    {
                        componentsout = "";
                        foreach (var c in selectedComponents)
                            componentsout += c.name + ", ";
                        componentsout = componentsout.Remove(componentsout.Count() - 2);
                    }
                    ChangeComponents.Content = componentsout;

                }
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1);
            }
        }

        private List<User> getAssigneesProj()
        {
            List<User> userlist = new List<User>();
            
            var maxresults = 1000;
            for (var i = 0; i < 100; i++)
            {
                var apicall = "user/assignable/search?project=" +
                          ((Project)projCombo.SelectedItem).key + "&maxResults=" + maxresults + "&startAt=" + (i * maxresults);
                var request = new RestRequest(apicall, Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.RequestFormat = Arup.RestSharp.DataFormat.Json;
                request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
                var response = JiraClient.Client.Execute<List<User>>(request);
                if (!RestCallback.Check(response) || !response.Data.Any())
                    break;

                userlist.AddRange(response.Data);
                if (response.Data.Count < maxresults)
                    break;
            }
            return userlist;
        }

        private List<Classes.Component> getComponents()
        {
            List<Classes.Component> components = new List<Component>();

            try
            {
                var request = new RestRequest("project/" + ((Project)projCombo.SelectedItem).key + "/components", Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.RequestFormat = Arup.RestSharp.DataFormat.Json;
                request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                var response = JiraClient.Client.Execute<List<Classes.Component>>(request);
                if (!RestCallback.Check(response) || !response.Data.Any())
                    return components;

                components.AddRange(response.Data);
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1);
            }

            return components;
        }

        private bool isAnyProjectSelected() 
        {
            if (projCombo.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a project first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }

            return false;
        }
    }
}
