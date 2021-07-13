using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace ARUP.IssueTracker.Revit.Entry
{

	/// <summary>
	/// Obfuscation Ignore for External Interface
	/// </summary>
	[Obfuscation(Exclude = true, ApplyToMembers = false)]
	[Transaction(TransactionMode.Manual)]
	public class AppMain : IExternalApplication
	{
		private string _path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		#region Revit IExternalApplciation Implementation

		/// <summary>
		/// Startup
		/// </summary>
		/// <param name="application"></param>
		/// <returns></returns>
		public Result OnStartup(UIControlledApplication application)
		{

			try
			{
#if REVIT2014
                string versionNumber = "2014";
#elif REVIT2015
                string versionNumber = "2015";
#elif REVIT2016
                string versionNumber = "2016";
#elif REVIT2017
                string versionNumber = "2017";
#elif REVIT2018
                string versionNumber = "2018";
#elif REVIT2019
                string versionNumber = "2019";
#elif REVIT2020
				string versionNumber = "2020";

#elif REVIT2021
				string versionNumber = "2021";

#elif REVIT2022
				string versionNumber = "2022";
#endif
				// Version
				if (!application.ControlledApplication.VersionName.Contains(versionNumber))
				{
					using (TaskDialog td = new TaskDialog("Cannot Continue"))
					{
						td.TitleAutoPrefix = false;
						td.MainInstruction = "Incompatible Revit Version";
                        td.MainContent = "This Add-In was built for Revit 2014, 2015 and 2016, please find the Arup Issue Tracker group in Yammer for assistance...";
						td.Show();
					}
					return Result.Cancelled;
				}

				// Master Tab 
				const string c_tabName = "CASE";

				try
				{
					// Create the Tab
					application.CreateRibbonTab(c_tabName);
				}
				catch { }

				// Assembly Paths
				string m_issuetracker = Path.Combine(ProgramFilesx86(), "CASE", "ARUP Issue Tracker", "ARUP.IssueTracker.dll");

				// Check that File Exists
				if (!File.Exists(m_issuetracker))
				{
					using (TaskDialog td = new TaskDialog("Cannot Continue"))
					{
						td.TitleAutoPrefix = false;
						td.MainInstruction = "Required Issue Tracker Library Not Found";
						td.MainContent = m_issuetracker;
						td.Show();
					}
					return Result.Cancelled;
				}

				// Load Assemblies
				Assembly.LoadFrom(m_issuetracker);

				// Tab
				RibbonPanel m_panel = application.CreateRibbonPanel(c_tabName, "Arup Issue Tracker");

				// Button Data
				PushButton m_pushButton = m_panel.AddItem(new PushButtonData("Issue Tracker",
																								 "Issue Tracker",
                                                                                                 Path.Combine(_path, string.Format("ARUP.IssueTracker.Revit.{0}.dll", versionNumber)),
																								 "ARUP.IssueTracker.Revit.Entry.CmdMain")) as PushButton;

				// Images and Tooltip
				if (m_pushButton != null)
				{
					m_pushButton.Image = LoadPngImgSource("ARUP.IssueTracker.Assets.ARUPIssueTrackerIcon16x16.png", m_issuetracker);
					m_pushButton.LargeImage = LoadPngImgSource("ARUP.IssueTracker.Assets.ARUPIssueTrackerIcon32x32.png", m_issuetracker);
					m_pushButton.ToolTip = "Arup Issue Manager";
				}

                // Initiate RestSharp first to avoid the conflict with Dynamo
                // string restSharpDllPath = Path.Combine(ProgramFilesx86(), "CASE", "ARUP Issue Tracker", "RestSharp.dll");
                // Assembly.LoadFrom(restSharpDllPath);

            }
			catch (Exception ex1)
			{
				MessageBox.Show("exception: " + ex1);
				return Result.Failed;
			}

			return Result.Succeeded;
		}

		/// <summary>
		/// Shut Down
		/// </summary>
		/// <param name="application"></param>
		/// <returns></returns>
		public Result OnShutdown(UIControlledApplication application)
		{
			return Result.Succeeded;
		}

		#endregion

		#region Private Members

		/// <summary>
		/// Get System Architecture
		/// </summary>
		/// <returns></returns>
		static string ProgramFilesx86()
		{
			if (8 == IntPtr.Size || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
				return Environment.GetEnvironmentVariable("ProgramFiles(x86)");

			return Environment.GetEnvironmentVariable("ProgramFiles");
		}


		/// <summary>
		/// Load an Image Source from File
		/// </summary>
		/// <param name="sourceName"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		private ImageSource LoadPngImgSource(string sourceName, string path)
		{

			try
			{
				// Assembly & Stream
				Assembly m_assembly = Assembly.LoadFrom(Path.Combine(path));
				Stream m_icon = m_assembly.GetManifestResourceStream(sourceName);

				// Decoder
				PngBitmapDecoder m_decoder = new PngBitmapDecoder(m_icon, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

				// Source
				ImageSource m_source = m_decoder.Frames[0];
				return (m_source);

			}
			catch { }

			// Fail
			return null;

		}

		#endregion

	}
}