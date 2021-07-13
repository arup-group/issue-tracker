using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace ARUP.IssueTracker.Revit.Entry
{

	/// <summary>
	/// Obfuscation Ignore for External Interface
	/// </summary>
	[Obfuscation(Exclude = true, ApplyToMembers = false)]
	[Transaction(TransactionMode.Manual)]
	[Regeneration(RegenerationOption.Manual)]
	public class CmdMain : IExternalCommand
	{
		internal static CmdMain ThisCmd = null;
		private static bool _isRunning;
		private static AppIssueTracker _appIssueTracker;

		/// <summary>
		/// Main Command Entry Point
		/// </summary>
		/// <param name="commandData"></param>
		/// <param name="message"></param>
		/// <param name="elements"></param>
		/// <returns></returns>
		public	Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
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
				if (!commandData.Application.Application.VersionName.Contains(versionNumber))
				{
					using (TaskDialog td = new TaskDialog("Cannot Continue"))
					{
						td.TitleAutoPrefix = false;
						td.MainInstruction = "Incompatible Revit Version";
                        td.MainContent = string.Format("This Add-In was built for Revit {0}, please find the Arup Issue Tracker group in Yammer for assistance...", versionNumber);
						td.Show();
					}
					return Result.Cancelled;
				}

				// Form Running?
				if (_isRunning && _appIssueTracker != null && _appIssueTracker.RvtWindow.IsLoaded)
				{
					_appIssueTracker.Focus();
					return Result.Succeeded;
				}

				_isRunning = true;

				ThisCmd = this;
				_appIssueTracker = new AppIssueTracker();
				_appIssueTracker.ShowForm(commandData.Application);

                // register a document closed event
                commandData.Application.Application.DocumentClosed += Application_DocumentClosed;

				return Result.Succeeded;

			}
			catch (Exception e)
			{
				message = e.Message;
				return Result.Failed;
			}

		}

        void Application_DocumentClosed(object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
        {
            _appIssueTracker.CloseForm();
        }

	}

}