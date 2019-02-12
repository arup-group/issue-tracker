using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
//using System.Drawing;
using ARUP.IssueTracker.Windows;
using System.IO;
using System.Collections.ObjectModel;
using ARUP.IssueTracker.Classes;
using Autodesk.AutoCAD.ApplicationServices;

namespace ARUP.IssueTracker.Civil3D
{
    /// <summary>
    /// Interaction logic for AddIssueCivil3D.xaml
    /// </summary>
    public partial class AddIssueCivil3D : Window
    {
        public string snapshotPath;
        private string[] visuals = { "FlatColors", "HLR", "Realistic", "RealisticWithEdges", "Rendering", "Shading", "ShadingWithEdges", "Wireframe" };
        private string[] statuses = { "Error", "Warning", "Info", "Unknown" };
        private ObservableCollection<Issuetype> typesCollection = new ObservableCollection<Issuetype>();
        private ObservableCollection<Component> compCollection = new ObservableCollection<Component>();
        private ObservableCollection<Priority> PrioritiesCollection = new ObservableCollection<Priority>();
        private List<User> assignees =  new List<User>();
        public List<Component> SelectedComponents = new List<Component>();
        private Document doc;

        public AddIssueCivil3D(Document doc, string folder, ObservableCollection<Issuetype> _typesCollection,
            List<User> _assignees, ObservableCollection<Component> _compCollection, ObservableCollection<Priority> _PrioritiesCollection, bool comp, bool prior, bool assign)
        {
            try
            {
                this.doc = doc;
                snapshotPath = System.IO.Path.Combine(folder, "snapshot.png");
                InitializeComponent();
                TitleBox.Focus();
                
                //comboVisuals.ItemsSource = visuals;
                //comboStatuses.ItemsSource = statuses;
                //comboStatuses.SelectedIndex = 3;

                if (null != _typesCollection) { 
                    typesCollection = _typesCollection;
                    issueTypeCombo.ItemsSource = typesCollection;
                    issueTypeCombo.SelectedIndex = 0;
                }
                if (!comp)
                {
                    compCollection = _compCollection;
                } else
                    ComponentsStack.Visibility = System.Windows.Visibility.Collapsed;

                
                if (!assign && null != _assignees)
                {
                    assignees = _assignees;
                } else
                    assigneeStack.Visibility = System.Windows.Visibility.Collapsed;
                if (!prior && null != _PrioritiesCollection)
                {
                    PrioritiesCollection = _PrioritiesCollection;
                    priorityCombo.ItemsSource = PrioritiesCollection;
                    priorityCombo.SelectedIndex = 0;
                }
                else
                    PriorityStack.Visibility = System.Windows.Visibility.Collapsed;
                

                //select current visual style
                //string currentV = null; // FIXME
                //for (int i = 0; i < comboVisuals.Items.Count; i++)
                //{
                //    if (comboVisuals.Items[i].ToString() == currentV)
                //    {
                //        comboVisuals.SelectedIndex = i;
                //    }
                //}


                updateImage();
            }
            catch (System.Exception ex1)
            {

            }

        }

        private void updateImage()
        {
            try
            {
                SnapshotImg.Source = null;

                // FIXME if necessary for changing view style

                BitmapImage source = new BitmapImage();
                source.BeginInit();
                source.UriSource = new Uri(snapshotPath);
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                source.EndInit();
                SnapshotImg.Source = source;

                PathLabel.Content = "";
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }

        }

        //private void comboVisuals_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {                
        //        updateImage();
        //    }
        //    catch (System.Exception ex1)
        //    {
        //        MessageBox.Show("exception: " + ex1, "Error!");
        //    }

        //}

        //LOAD EXTERNAL IMAGE
        private void Button_LoadImage(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            //  openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
            openFileDialog1.RestoreDirectory = true;
            Nullable<bool> result = openFileDialog1.ShowDialog(); // Show the dialog.

            if (result == true) // Test result.
            {
                try
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(openFileDialog1.FileName);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.EndInit();
                    BitmapSource a = ConvertBitmapTo96DPI(image);
                    int width = (int)a.Width;
                    int height = (int)a.Height;
                    if (width > 1500 || image.Height > 1500)
                    {
                        string size = width.ToString() + "x" + height.ToString();
                        int newWidth = 1500;
                        float scale = (float)newWidth / ((float)width / (float)height);
                        int newHeight = Convert.ToInt32(scale);

                        MessageBoxResult answer = MessageBox.Show("Image size is " + size + ", "
                            + "such a big image could increase A LOT the BCF file size. "
                        + "Do you want me to resize it to " + newWidth.ToString() + "x" + newHeight.ToString() + " for you?", "Attention!",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (answer == MessageBoxResult.Yes)
                        {
                            width = newWidth;
                        }

                        /*
                        MessageBox mainDialog = new MessageBox("Attention!");
                        mainDialog.MainInstruction = "Attention!";
                        mainDialog.MainContent =
                            "Image size is " + size + ", "
                            + "such a big image could increase A LOT the BCF file size. "
                        + "Do you want me to resize it to " + newWidth.ToString() + "x" + newHeight.ToString() + " for you?";
                        mainDialog.CommonButtons = MessageBoxCommonButtons.No ^ MessageBoxCommonButtons.Yes;
                        mainDialog.DefaultButton = MessageBoxResult.Yes;
                        MessageBoxResult tResult = mainDialog.Show();
                        
                        //ONLY IF NECESSARY I RESIZE
                        if (MessageBoxResult.Yes == tResult)
                        {
                           width = newHeight;
                        }*/
                    }
                    byte[] imageBytes = LoadImageData(openFileDialog1.FileName);
                    ImageSource imageSource = CreateImage(imageBytes, width, 0);
                    imageBytes = GetEncodedImageData(imageSource, ".jpg");
                    SaveImageData(imageBytes, snapshotPath);
                    //VISUALIZE IMAGE
                    BitmapImage source = new BitmapImage();
                    source.BeginInit();
                    source.UriSource = new Uri(snapshotPath);
                    source.CacheOption = BitmapCacheOption.OnLoad;
                    source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    source.EndInit();
                    SnapshotImg.Source = source;
                    PathLabel.Content = openFileDialog1.FileName;
                }
                catch (System.Exception ex1)
                {
                    MessageBox.Show("exception: " + ex1, "Error!");
                }
            }
        }

        public static BitmapSource ConvertBitmapTo96DPI(BitmapImage bitmapImage)
        {
            try
            {
                double dpi = 96;
                int width = bitmapImage.PixelWidth;
                int height = bitmapImage.PixelHeight;

                int stride = width * 4; // 4 bytes per pixel
                byte[] pixelData = new byte[stride * height];
                bitmapImage.CopyPixels(pixelData, stride, 0);

                return BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, pixelData, stride);
            }

            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
            return null;
        }

        private static byte[] LoadImageData(string filePath)
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                byte[] imageBytes = br.ReadBytes((int)fs.Length);
                br.Close();
                fs.Close();
                return imageBytes;
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
            return null;
        }

        private static ImageSource CreateImage(byte[] imageData, int decodePixelWidth, int decodePixelHeight)
        {
            try
            {
                if (imageData == null) return null;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                if (decodePixelWidth > 0)
                {
                    result.DecodePixelWidth = decodePixelWidth;
                }
                if (decodePixelHeight > 0)
                {
                    result.DecodePixelHeight = decodePixelHeight;
                }
                result.StreamSource = new MemoryStream(imageData);
                result.CreateOptions = BitmapCreateOptions.None;
                result.CacheOption = BitmapCacheOption.Default;
                result.EndInit();
                return result;
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
            return null;
        }

        private static void SaveImageData(byte[] imageData, string filePath)
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.Create,
                FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(imageData);
                bw.Close();
                fs.Close();
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
        }

        internal byte[] GetEncodedImageData(ImageSource image, string preferredFormat)
        {
            try
            {
                byte[] result = null;
                BitmapEncoder encoder = null;
                switch (preferredFormat.ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".tif":
                    case ".tiff":
                        encoder = new TiffBitmapEncoder();
                        break;
                    case ".gif":
                        encoder = new GifBitmapEncoder();
                        break;
                    case ".wmp":
                        encoder = new WmpBitmapEncoder();
                        break;
                }
                if (image is BitmapSource)
                {
                    MemoryStream stream = new MemoryStream();
                    encoder.Frames.Add(BitmapFrame.Create(image as BitmapSource));
                    encoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    result = new byte[stream.Length];
                    BinaryReader br = new BinaryReader(stream);
                    br.Read(result, 0, (int)stream.Length);
                    br.Close();
                    stream.Close();
                }
                return result;
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
            return null;
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Button_OK(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Please insert an Issue Title.", "Title required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void EditSnapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(snapshotPath))
                {
                    //edit snapshot
                    string editSnap = "mspaint";
                    Process paint = new Process();
                    // paint.Start(editSnap, "\"" + snapshot + "\"");
                    ProcessStartInfo paintInfo = new ProcessStartInfo(editSnap, "\"" + snapshotPath + "\"");

                    paintInfo.UseShellExecute = false;
                    //navisworksStartInfo.RedirectStandardOutput = true;

                    paint.StartInfo = paintInfo;
                    paint.Start();

                    paint.WaitForExit();
                    Refresh_Click(null, null);


                }
                else
                    MessageBox.Show("Snapshot is not a valit image, please try again.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapImage source = new BitmapImage();
                source.BeginInit();
                source.UriSource = new Uri(snapshotPath);
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                source.EndInit();
                SnapshotImg.Source = source;
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1, "Error!");
            }
        }


        private void ChangeAssign_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // = getAssigneesIssue();
                if (!assignees.Any())
                {
                    MessageBox.Show("You don't have permission to Assign people to this Issue");
                    return;
                    //jira.issuesCollection[jiraPan.issueList.SelectedIndex].transitions = response2.Data.transitions;
                }
                ChangeAssignee cv = new ChangeAssignee(); cv.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                cv.SetList(assignees);
                cv.valuesList.SelectedIndex = (ChangeAssign.Content.ToString() != "none") ? IndexByName.Get(ChangeAssign.Content.ToString(), "name", assignees) : -1;

           
                cv.Title = "Assign to";
                cv.ShowDialog();
                if (cv.DialogResult.HasValue && cv.DialogResult.Value)
                {
                    User assign = (cv.valuesList.SelectedIndex >= cv.valuesList.Items.Count || cv.valuesList.SelectedIndex == -1) ? null : (User)cv.valuesList.SelectedItem;
                    ChangeAssign.Content = (assign!=null) ? assign.displayName : "none";
                    ChangeAssign.Tag = (assign != null) ? assign.name : null;
                }
            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1);
            }
        }

        private void ChangeComponents_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {

                ChangeValue cv = new ChangeValue();
                cv.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                List<Component> components = compCollection.ToList<Component>();
                cv.valuesList.ItemsSource = components;
                cv.valuesList.SelectionMode = SelectionMode.Multiple;

                cv.Title = "Change Components";
                DataTemplate componentTemplate = cv.FindResource("componentTemplate") as DataTemplate;
                cv.valuesList.ItemTemplate = componentTemplate;
                cv.valuesList.SelectedIndex = -1;
                if (SelectedComponents != null && SelectedComponents.Any())
                {

                    foreach (var o in SelectedComponents)
                    {
                        var selindex =components.IndexOf(o);
                        if (selindex != -1)
                            cv.valuesList.SelectedItems.Add(cv.valuesList.Items[selindex]);
                    }
                }
               
                    
                // ChangeStatus ChangSt = new ChangeStatus(jira.issuesCollection[jiraPan.issueList.SelectedIndex].transitions);
                cv.ShowDialog();
                if (cv.DialogResult.HasValue && cv.DialogResult.Value)
                {
                    SelectedComponents = new List<Component>();

                    foreach (var c in cv.valuesList.SelectedItems)
                    {
                        Component cc = c as Component;
                        SelectedComponents.Add(cc);

                    }

                    string componentsout = "none";

                    if (SelectedComponents != null && SelectedComponents.Any())
                    {
                        componentsout = "";
                        foreach (var c in SelectedComponents)
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
    }
}
