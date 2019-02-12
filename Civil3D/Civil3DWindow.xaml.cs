using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.IO;
using ARUP.IssueTracker.Classes;
using ARUP.IssueTracker.Classes.BCF2;
using System.ComponentModel;
using ARUP.IssueTracker.Windows;
using System.Windows.Controls;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Interop;

namespace ARUP.IssueTracker.Civil3D
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
    public partial class Civil3DWindow : Window
  {
    private CommentController commentController;
    private ComponentController componentController;
    private Document doc;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="_uiapp"></param>
    /// <param name="exEvent"></param>
    /// <param name="handler"></param>
    public Civil3DWindow(Document doc)
    {
      try
      {
        InitializeComponent();
        this.Title = AITPlugin.getAutoCADProductName() + " | Arup Issue Tracker by CASE";

        mainPan.jiraPan.AddIssueBtn.Click += new RoutedEventHandler(AddIssueJira);
        mainPan.bcfPan.AddIssueBtn.Click += new RoutedEventHandler(AddIssueBCF);

        mainPan.bcfPan.open3dViewEvent += new RoutedEventHandler(Open3dViewBCF);
        mainPan.jiraPan.open3dViewEvent += new RoutedEventHandler(Open3dViewJira);

        // for ICommentController callback
        commentController = new CommentController(this);
        commentController.client = AuthoringTool.Civil3D;
        mainPan.jiraPan.AddCommBtn.Tag = commentController;
        mainPan.bcfPan.AddCommBtn.Tag = commentController;

        // for IComponentController
        componentController = new ComponentController(this);
        mainPan.componentController = this.componentController;

        // for open 3d view and show components
        //mainPan.jiraPan.open3dView.Visibility = System.Windows.Visibility.Visible;
        //mainPan.jiraPan.showComponents.Visibility = System.Windows.Visibility.Visible;
        //mainPan.bcfPan.isShowBcfFirstViewpointButtons = true;

        this.doc = doc;
      }

      catch (System.Exception ex1)
      {
        MessageBox.Show("exception: " + ex1);
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
          Tuple<Markup, Issue> tup = AddIssue(path, false);
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
          Tuple<Markup, Issue> tup = AddIssue(mainPan.jira.Bcf.TempPath, true);
        if (tup == null)
          return;
        Markup issue = tup.Item1;

          if (issue != null)
          {
              mainPan.jira.Bcf.Issues.Add(issue);
              mainPan.jira.Bcf.HasBeenSaved = false;
              mainPan.bcfPan.issueList.SelectedIndex = mainPan.jira.Bcf.Issues.Count - 1;
          }
      }

      catch (System.Exception ex1)
      {
        MessageBox.Show("exception: " + ex1);
      }
    }

    /// <summary>
    /// Add Issue
    /// </summary>
    /// <param name="path"></param>
    /// <param name="isBcf"></param>
    /// <returns></returns>
    private Tuple<Markup, Issue> AddIssue(string path, bool isBcf)
    {
      try
      {
        if (doc.Editor.GetCurrentView().IsPaperspaceView)
        {
            MessageBox.Show("Please switch to model space as paper space is not supported.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }

        Markup issue = new Markup(DateTime.Now);

        string folderIssue = Path.Combine(path, issue.Topic.Guid);
        if (!Directory.Exists(folderIssue))
          Directory.CreateDirectory(folderIssue);

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

        // get snapshot by AITSNAPSHOT command
        string snapshotPath = Path.Combine(folderIssue, "snapshot.png");
        AcadApplication acadApp = (AcadApplication)Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
        acadApp.ActiveDocument.SendCommand(string.Format("_.AITSNAPSHOT {0}\n", snapshotPath));

        AddIssueCivil3D aic = new AddIssueCivil3D(doc, folderIssue, types, assignees, components, priorities, noCom, noPrior, noAssign);
        aic.Title = "Add Jira Issue";
        if (!isBcf)
        {
            aic.JiraFieldsBox.Visibility = System.Windows.Visibility.Visible;
            aic.VerbalStatus.Visibility = System.Windows.Visibility.Collapsed;
            aic.BcfFieldsBox.Visibility = System.Windows.Visibility.Collapsed;
        }
        else 
        {
            aic.JiraFieldsBox.Visibility = System.Windows.Visibility.Collapsed;
            aic.BcfFieldsBox.Visibility = System.Windows.Visibility.Visible;
        }

        aic.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        aic.ShowDialog();
          if (aic.DialogResult.HasValue && aic.DialogResult.Value)
          {
              ViewPoint vp = new ViewPoint(true);
              vp.SnapshotPath = snapshotPath;
              int elemCheck = 2;
              //if (aic.all.IsChecked.Value)
              //    elemCheck = 0;
              //else if (aic.selected.IsChecked.Value)
              //    elemCheck = 1;
              vp.VisInfo = generateViewpoint(elemCheck);

              //Add annotations for description with snapshot/viewpoint
              StringBuilder descriptionText = new StringBuilder();
              if (!string.IsNullOrWhiteSpace(aic.CommentBox.Text))
              {
                  descriptionText.AppendLine(aic.CommentBox.Text);
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
                  issueJira.fields.issuetype =  (Issuetype) aic.issueTypeCombo.SelectedItem;
                  issueJira.fields.priority = (Priority) aic.priorityCombo.SelectedItem;
                  if (!string.IsNullOrEmpty(aic.ChangeAssign.Content.ToString()) &&
                      aic.ChangeAssign.Content.ToString() != "none")
                  {
                      issueJira.fields.assignee = new User();
                      issueJira.fields.assignee.name = aic.ChangeAssign.Tag.ToString();
                  }

                  if (aic.SelectedComponents != null && aic.SelectedComponents.Any())
                  {
                      issueJira.fields.components = aic.SelectedComponents;
                  }

                  if (!string.IsNullOrWhiteSpace(aic.JiraLabels.Text))
                  {
                      var labels = aic.JiraLabels.Text.Split(',').ToList();
                      labels.ForEach(s => s.Trim());
                      issueJira.fields.labels = labels;
                  }
              }
              
              issue.Viewpoints.Add(vp);
              issue.Topic.Title = aic.TitleBox.Text;
              issue.Topic.Description = descriptionText.ToString().Trim();
              issue.Topic.AssignedTo = aic.BcfAssignee.Text;
              issue.Topic.CreationAuthor = string.IsNullOrWhiteSpace(MySettings.Get("username")) ? MySettings.Get("BCFusername") : MySettings.Get("username");
              issue.Topic.Priority = aic.BcfPriority.Text;
              issue.Topic.TopicStatus = aic.VerbalStatus.Text;
              issue.Topic.TopicType = aic.BcfIssueType.Text;
              if (!string.IsNullOrWhiteSpace(aic.BcfLabels.Text))
              {
                  var labels = aic.BcfLabels.Text.Split(',').ToList();
                  labels.ForEach(s => s.Trim());
                  issue.Topic.Labels = labels.ToArray();
              }

              issue.Header[0].IfcProject = doc.Database.FingerprintGuid;
              issue.Header[0].Filename = doc.Database.OriginalFileName;
              issue.Header[0].Date = DateTime.Now;

              return new Tuple<Markup, Issue>(issue, issueJira);
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
      return null;

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
        try
        {
            if (doc.Editor.GetCurrentView().IsPaperspaceView)
            {
                MessageBox.Show("This operation is not allowed in paper space.\nPlease go to model space and retry.",
                    "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get the current database
            Database acCurDb = doc.Database;
            double unitFactor = UnitsConverter.GetConversionFactor(UnitsValue.Meters, acCurDb.Insunits);

            Point3d wcsCenter;
            Vector3d viewDirection, upVector;
            double zoomValue;

            // IS ORTHOGONAL
            if (v.OrthogonalCamera != null)
            {
                if (v.OrthogonalCamera.CameraViewPoint == null || v.OrthogonalCamera.CameraUpVector == null || v.OrthogonalCamera.CameraDirection == null)
                    return;

                wcsCenter = new Point3d(v.OrthogonalCamera.CameraViewPoint.X, v.OrthogonalCamera.CameraViewPoint.Y, v.OrthogonalCamera.CameraViewPoint.Z);
                viewDirection = new Vector3d(v.OrthogonalCamera.CameraDirection.X, v.OrthogonalCamera.CameraDirection.Y, v.OrthogonalCamera.CameraDirection.Z);
                upVector = new Vector3d(v.OrthogonalCamera.CameraUpVector.X, v.OrthogonalCamera.CameraUpVector.Y, v.OrthogonalCamera.CameraUpVector.Z);
                zoomValue = v.OrthogonalCamera.ViewToWorldScale;
            }
            else if (v.PerspectiveCamera != null)
            {
                if (v.PerspectiveCamera.CameraViewPoint == null || v.PerspectiveCamera.CameraUpVector == null || v.PerspectiveCamera.CameraDirection == null)
                    return;

                wcsCenter = new Point3d(v.PerspectiveCamera.CameraViewPoint.X, v.PerspectiveCamera.CameraViewPoint.Y, v.PerspectiveCamera.CameraViewPoint.Z);
                viewDirection = new Vector3d(v.PerspectiveCamera.CameraDirection.X, v.PerspectiveCamera.CameraDirection.Y, v.PerspectiveCamera.CameraDirection.Z);
                upVector = new Vector3d(v.PerspectiveCamera.CameraUpVector.X, v.PerspectiveCamera.CameraUpVector.Y, v.PerspectiveCamera.CameraUpVector.Z);
                zoomValue = v.PerspectiveCamera.FieldOfView;
            }
            else 
            {
                MessageBox.Show("No camera information was found within this viewpoint.",
                    "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Get the current view
                using (ViewTableRecord currentView = doc.Editor.GetCurrentView())
                {
                    // Calculate the ratio between the width and height of the current view
                    double dViewRatio;
                    dViewRatio = (currentView.Width / currentView.Height);

                    // set target
                    wcsCenter = wcsCenter * unitFactor;
                    currentView.Target = wcsCenter;

                    // set direction
                    currentView.ViewDirection = viewDirection.GetNormal().Negate();

                    // Set scale
                    currentView.Height = zoomValue * unitFactor;
                    currentView.Width = zoomValue * dViewRatio * unitFactor;

                    Matrix3d matWCS2DCS;
                    matWCS2DCS = Matrix3d.PlaneToWorld(currentView.ViewDirection);
                    matWCS2DCS = Matrix3d.Displacement(currentView.Target - Point3d.Origin) * matWCS2DCS;
                    matWCS2DCS = Matrix3d.Rotation(-currentView.ViewTwist,
                                                    currentView.ViewDirection,
                                                    currentView.Target) * matWCS2DCS;
                    matWCS2DCS = matWCS2DCS.Inverse();

                    // set up vector
                    currentView.ViewTwist = Math.Asin(-upVector.GetNormal().TransformBy(matWCS2DCS).X);

                    // Set the center of the view                    
                    Point3d dcsCenter = wcsCenter.TransformBy(matWCS2DCS);
                    currentView.CenterPoint = new Point2d(dcsCenter.X, dcsCenter.Y);

                    // Set the current view
                    doc.Editor.SetCurrentView(currentView);
                }

                // Commit the changes
                acTrans.Commit();
            }
        }
        catch (System.Exception ex1)
        {
            MessageBox.Show("exception: " + ex1, "Error!");
        }
    }

    #region viewpoint operations

    /// <summary>
    /// Generate Viewpoint
    /// </summary>
    /// <param name="elemCheck"></param>
    /// <returns></returns>
    public VisualizationInfo generateViewpoint(int elemCheck)
    {
        try
        {
            // get current view
            Database acCurDb = doc.Database;
            ViewTableRecord currentView = doc.Editor.GetCurrentView();
            double unitFactor = UnitsConverter.GetConversionFactor(acCurDb.Insunits, UnitsValue.Meters);

            // camera direction
            Vector3d direction = currentView.ViewDirection.Negate();

            // camera scale
            double h = currentView.Height * unitFactor;
            double w = currentView.Width * unitFactor;

            // transform from DCS to WCS
            Matrix3d matDCS2WCS;
            matDCS2WCS = Matrix3d.PlaneToWorld(currentView.ViewDirection);
            matDCS2WCS = Matrix3d.Displacement(currentView.Target - Point3d.Origin) * matDCS2WCS;
            matDCS2WCS = Matrix3d.Rotation(-currentView.ViewTwist,
                                            currentView.ViewDirection,
                                            currentView.Target) * matDCS2WCS;            

            // camera location
            Point3d centerPointDCS = new Point3d(currentView.CenterPoint.X, currentView.CenterPoint.Y, 0.0);
            Point3d centerWCS = centerPointDCS.TransformBy(matDCS2WCS);
            Point3d cameraLocation = centerWCS.Subtract(direction) * unitFactor;

            // camera up vector
            Vector3d upVector = new Vector3d(-Math.Sin(currentView.ViewTwist), Math.Cos(currentView.ViewTwist), 0.0).TransformBy(matDCS2WCS).GetNormal();

            // set up BCF viewpoint
            VisualizationInfo v = new VisualizationInfo();            

            if (currentView.PerspectiveEnabled)
            {
                v.PerspectiveCamera = new PerspectiveCamera();
                v.PerspectiveCamera.CameraViewPoint.X = cameraLocation.X;
                v.PerspectiveCamera.CameraViewPoint.Y = cameraLocation.Y;
                v.PerspectiveCamera.CameraViewPoint.Z = cameraLocation.Z;
                v.PerspectiveCamera.CameraUpVector.X = upVector.X;
                v.PerspectiveCamera.CameraUpVector.Y = upVector.Y;
                v.PerspectiveCamera.CameraUpVector.Z = upVector.Z;
                v.PerspectiveCamera.CameraDirection.X = direction.GetNormal().X;
                v.PerspectiveCamera.CameraDirection.Y = direction.GetNormal().Y;
                v.PerspectiveCamera.CameraDirection.Z = direction.GetNormal().Z;
                v.PerspectiveCamera.FieldOfView = 40;
            }
            else 
            {
                v.OrthogonalCamera = new OrthogonalCamera();
                v.OrthogonalCamera.CameraViewPoint.X = cameraLocation.X;
                v.OrthogonalCamera.CameraViewPoint.Y = cameraLocation.Y;
                v.OrthogonalCamera.CameraViewPoint.Z = cameraLocation.Z;
                v.OrthogonalCamera.CameraUpVector.X = upVector.X;
                v.OrthogonalCamera.CameraUpVector.Y = upVector.Y;
                v.OrthogonalCamera.CameraUpVector.Z = upVector.Z;
                v.OrthogonalCamera.CameraDirection.X = direction.GetNormal().X;
                v.OrthogonalCamera.CameraDirection.Y = direction.GetNormal().Y;
                v.OrthogonalCamera.CameraDirection.Z = direction.GetNormal().Z;
                v.OrthogonalCamera.ViewToWorldScale = h;
            }

            return v;
        }
        catch (System.Exception ex1)
        {
            MessageBox.Show("exception: " + ex1, "Error!");
        }
        return null;
    }

    #endregion
    
    /// <summary>
    /// passing event to the user control
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Closing(object sender, CancelEventArgs e)
    {
      e.Cancel = mainPan.onClosing(e);
    }

  }
}