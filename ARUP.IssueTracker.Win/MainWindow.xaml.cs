using System;
using System.Windows;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ARUP.IssueTracker.Classes.BCF2;
using ARUP.IssueTracker.Classes;
using System.IO.Pipes;
using System.Text;
using ARUP.IssueTracker.IPC;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ARUP.IssueTracker.Win
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private string ipcPipeName;
    public MainWindow()
    {
        InitializeComponent();

        mainPan.mainWindow = this;
        // Buttons, etc.
        mainPan.setButtonVisib(false, false, false);
        string[] args = Environment.GetCommandLineArgs();
        if (args.Length == 4)
        {
            if (args[1] == "IPC")
            {
                this.Title += " | " + args[2];
                ipcPipeName = args[3];
                mainPan.setButtonVisib(true, true, true);
                ipcSetup();
            }
        }
        else if (args.Length == 2)
        {
            mainPan.OpenBCF(args[1].ToString());
        }
    }

    private void ipcSetup()
    {
        mainPan.jiraPan.AddIssueBtn.Click += new RoutedEventHandler(AddIssueJira);
        mainPan.bcfPan.AddIssueBtn.Click += new RoutedEventHandler(AddIssueBCF);

        mainPan.bcfPan.open3dViewEvent += new RoutedEventHandler(Open3dViewBCF);
        mainPan.jiraPan.open3dViewEvent += new RoutedEventHandler(Open3dViewJira);

        // for selecting attached elements
        mainPan.selectElements = this.SelectElements;
    }

    /// <summary>
    /// passing event to the user control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = mainPan.onClosing(e);
    }

    private void sendIpcRequest(IpcOperationType type, object data, Action<object> callback)
    {
        try
        {
            NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", ipcPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            // The connect function will indefinitely wait for the pipe to become available
            // If that is not acceptable specify a maximum waiting time (in ms)
            pipeStream.Connect(1000);

            string msg = IpcMessageStore.savePayload(type, data);

            byte[] _buffer = Encoding.UTF8.GetBytes(msg);
            pipeStream.BeginWrite(_buffer, 0, _buffer.Length, ipcAsyncCallback, new Tuple<NamedPipeClientStream, Action<object>>(pipeStream, callback));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Arup Issue Tracker could not finish the operation due to the following error:\n" + ex.ToString());
        }
    }

    private void ipcAsyncCallback(IAsyncResult iar)
    {
        try
        {
            // Get the pipe
            NamedPipeClientStream pipeStream = ((Tuple<NamedPipeClientStream, Action<object>>)iar.AsyncState).Item1;

            // End the write
            pipeStream.EndWrite(iar);

            byte[] buffer = new byte[1024];
            pipeStream.Read(buffer, 0, 1024);
            string jsonMsg = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            IpcOperationType type = IpcMessageStore.getIpcOperationType(jsonMsg);

            if(type == IpcOperationType.AddIssueResponse)
            {
                AddIssueResponse response = IpcMessageStore.getPayload<AddIssueResponse>(jsonMsg);

                // return payload object in callback in the same UI thread
                this.Dispatcher.Invoke(() => ((Tuple<NamedPipeClientStream, Action<object>>)iar.AsyncState).Item2(response));
            }
            else if (type == IpcOperationType.Invalid)                    
            {
                // extends other types of IPC responses here...
            }

            pipeStream.Flush();
            pipeStream.Close();
            pipeStream.Dispose();
        }
        catch (Exception oEX)
        {
            MessageBox.Show(oEX.Message);
        }
    }

    /// <summary>
    /// Add Issue to Jira
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AddIssueJira(object sender, EventArgs e)
    {
        try
        {
            string path = Path.Combine(Path.GetTempPath(), "BCFtemp", Path.GetRandomFileName());
            AddIssue(path, false, (tup) =>
            {
                if (tup == null)
                    return;
                Markup issue = tup.Item1;
                Issue issueJira = tup.Item2;

                List<Markup> issues = new List<Markup>();
                List<Issue> issuesJira = new List<Issue>();

                issues.Add(issue);
                issuesJira.Add(issueJira);

                if (issue != null)
                    mainPan.doUploadIssue(issues, path, true, mainPan.jiraPan.projIndex, issuesJira);
            });            
        }

        catch (System.Exception ex1)
        {
            MessageBox.Show("exception: " + ex1);
        }

    }

    /// <summary>
    /// Add Issue to BCF
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AddIssueBCF(object sender, EventArgs e)
    {
        try
        {
            AddIssue(mainPan.jira.Bcf.TempPath, true, (tup) => {
                if (tup == null)
                return;
                Markup issue = tup.Item1;

                if (issue != null)
                {
                    mainPan.jira.Bcf.Issues.Add(issue);
                    mainPan.jira.Bcf.HasBeenSaved = false;
                    mainPan.bcfPan.issueList.SelectedIndex = mainPan.jira.Bcf.Issues.Count - 1;
                }
            });            
        }

        catch (System.Exception ex1)
        {
            MessageBox.Show("exception: " + ex1);
        }
    }

    /// <summary>
    /// Add Issue
    /// </summary>
    private void AddIssue(string path, bool isBcf, Action<Tuple<Markup, Issue>> callback)
    {
        sendIpcRequest(IpcOperationType.AddIssueRequest, null, (o) => {
            try
            {
                AddIssueResponse ipcResponse = o as AddIssueResponse;

                if (!ipcResponse.isValidRequest)
                {
                    MessageBox.Show("Please switch to model space as paper space is not supported.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }                

                Markup issue = new Markup(DateTime.Now);

                string folderIssue = Path.Combine(path, issue.Topic.Guid);
                if (!Directory.Exists(folderIssue))
                    Directory.CreateDirectory(folderIssue);

                // copy snapshot from temp file and delete temp file
                string snapshotPath = Path.Combine(folderIssue, "snapshot.png");
                File.Copy(ipcResponse.tempSnapshotPath, snapshotPath);
                File.Delete(ipcResponse.tempSnapshotPath);

                var types = new ObservableCollection<Issuetype>();
                var assignees = new List<User>();
                var components = new ObservableCollection<IssueTracker.Classes.Component>();
                var priorities = new ObservableCollection<Priority>();
                var noCom = true;
                var noPrior = true;
                var noAssign = true;

                if (!isBcf)
                {
                    types = mainPan.jira.TypesCollection;
                    assignees = mainPan.getAssigneesIssue();
                    components = mainPan.jira.ComponentsCollection;
                    priorities = mainPan.jira.PrioritiesCollection;
                    noCom =
                        mainPan.jira.ProjectsCollection[mainPan.jiraPan.projIndex].issuetypes[0].fields.components ==
                        null;
                    noPrior =
                        mainPan.jira.ProjectsCollection[mainPan.jiraPan.projIndex].issuetypes[0].fields.priority ==
                        null;
                    noAssign =
                        mainPan.jira.ProjectsCollection[mainPan.jiraPan.projIndex].issuetypes[0].fields.assignee ==
                        null;

                }

                AddIssueDialog aid = new AddIssueDialog(folderIssue, types, assignees, components, priorities, noCom, noPrior, noAssign);
                aid.Title = "Add Jira Issue";
                if (!isBcf)
                {
                    aid.JiraFieldsBox.Visibility = System.Windows.Visibility.Visible;
                    aid.VerbalStatus.Visibility = System.Windows.Visibility.Collapsed;
                    aid.BcfFieldsBox.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    aid.JiraFieldsBox.Visibility = System.Windows.Visibility.Collapsed;
                    aid.BcfFieldsBox.Visibility = System.Windows.Visibility.Visible;
                }

                aid.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                aid.ShowDialog();
                if (aid.DialogResult.HasValue && aid.DialogResult.Value)
                {
                    ViewPoint vp = new ViewPoint(true);
                    vp.SnapshotPath = snapshotPath;
                    //int elemCheck = 2;
                    //if (aic.all.IsChecked.Value)
                    //    elemCheck = 0;
                    //else if (aic.selected.IsChecked.Value)
                    //    elemCheck = 1;
                    vp.VisInfo = ipcResponse.visualizationInfo;

                    //Add annotations for description with snapshot/viewpoint
                    StringBuilder descriptionText = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(aid.CommentBox.Text))
                    {
                        descriptionText.AppendLine(aid.CommentBox.Text);
                    }
                    if (!isBcf)
                    {
                        if (vp.VisInfo != null)
                        {
                            descriptionText.AppendLine(string.Format("{{anchor:<Viewpoint>[^{0}]</Viewpoint>}}", "viewpoint.bcfv"));
                        }
                        if (!string.IsNullOrWhiteSpace(vp.SnapshotPath))
                        {
                            descriptionText.AppendLine(string.Format("!{0}|thumbnail!", "snapshot.png"));
                            descriptionText.AppendLine(string.Format("{{anchor:<Snapshot>[^{0}]</Snapshot>}}", "snapshot.png"));
                        }
                    }

                    Issue issueJira = new Issue();
                    if (!isBcf)
                    {
                        issueJira.fields = new Fields();
                        issueJira.fields.description = descriptionText.ToString().Trim();
                        issueJira.fields.issuetype = (Issuetype)aid.issueTypeCombo.SelectedItem;
                        issueJira.fields.priority = (Priority)aid.priorityCombo.SelectedItem;
                        if (!string.IsNullOrEmpty(aid.ChangeAssign.Content.ToString()) &&
                            aid.ChangeAssign.Content.ToString() != "none")
                        {
                            issueJira.fields.assignee = new User();
                            issueJira.fields.assignee.name = aid.ChangeAssign.Tag.ToString();
                        }

                        if (aid.SelectedComponents != null && aid.SelectedComponents.Any())
                        {
                            issueJira.fields.components = aid.SelectedComponents;
                        }

                        if (!string.IsNullOrWhiteSpace(aid.JiraLabels.Text))
                        {
                            var labels = aid.JiraLabels.Text.Split(',').ToList();
                            labels.ForEach(s => s.Trim());
                            issueJira.fields.labels = labels;
                        }
                    }

                    issue.Viewpoints.Add(vp);
                    issue.Topic.Title = aid.TitleBox.Text;
                    issue.Topic.Description = descriptionText.ToString().Trim();
                    issue.Topic.AssignedTo = aid.BcfAssignee.Text;
                    issue.Topic.CreationAuthor = string.IsNullOrWhiteSpace(MySettings.Get("username")) ? MySettings.Get("BCFusername") : MySettings.Get("username");
                    issue.Topic.Priority = aid.BcfPriority.Text;
                    issue.Topic.TopicStatus = aid.VerbalStatus.Text;
                    issue.Topic.TopicType = aid.BcfIssueType.Text;
                    if (!string.IsNullOrWhiteSpace(aid.BcfLabels.Text))
                    {
                        var labels = aid.BcfLabels.Text.Split(',').ToList();
                        labels.ForEach(s => s.Trim());
                        issue.Topic.Labels = labels.ToArray();
                    }

                    issue.Header[0].IfcProject = ipcResponse.documentGuid;
                    issue.Header[0].Filename = ipcResponse.documentName;
                    issue.Header[0].Date = DateTime.Now;

                    callback(new Tuple<Markup, Issue>(issue, issueJira));
                }
                else
                {
                    mainPan.DeleteDirectory(folderIssue);
                }

            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1);
            }
        });
    }

    /// <summary>
    /// Open 3D View - BCF
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Open3dViewBCF(object sender, EventArgs e)
    {
        try
        {
            VisualizationInfo VisInfo = (VisualizationInfo)((Button)sender).Tag;
            doOpen3DView(VisInfo);
        }
        catch (System.Exception ex1)
        {
            MessageBox.Show("exception: " + ex1);
        }
    }

    /// <summary>
    /// Open 3D View - Jira
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Open3dViewJira(object sender, RoutedEventArgs e)
    {
        try
        {
            string url = (string)((Button)sender).Tag;
            VisualizationInfo v = mainPan.getVisInfo(url);
            if (v == null)
            {
                MessageBox.Show("Failed to open the viewpoint", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            doOpen3DView(v);
        }
        catch (System.Exception ex1)
        {
            MessageBox.Show("exception: " + ex1);
        }
    }

    /// <summary>
    /// Open a 3D View
    /// </summary>
    /// <param name="v"></param>
    private void doOpen3DView(VisualizationInfo v)
    {
        sendIpcRequest(IpcOperationType.OpenViewpointRequest, v, null);
    }

    public void SelectElements(List<ARUP.IssueTracker.Classes.BCF2.Component> components)
    {
        sendIpcRequest(IpcOperationType.SelectElementsRequest, components, null);
    }

  }
}