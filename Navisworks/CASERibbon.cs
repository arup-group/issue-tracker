using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Navisworks.Api.Plugins;

namespace ARUP.IssueTracker.Navisworks
{
	/// <summary>
	/// Obfuscation Ignore for External Interface
	/// </summary>
	[Obfuscation(Exclude = true, ApplyToMembers = false)]
	[Plugin("ARUPIssueTrackerRibbon", "CASE", DisplayName = "CASE Design, Inc.")]
	[RibbonLayout("RibbonDefinition.xaml")]
	[RibbonTab("ID_casearup")]
	[Command("ID_arupissuetracker", DisplayName = "Arup Issue Tracker", Icon = "ARUPIssueTrackerIcon16x16.png", LargeIcon = "ARUPIssueTrackerIcon32x32.png", ToolTip = "Arup Issue Tracker", ExtendedToolTip = "Arup Issue Tracker by CASE")]
	public class CASERibbon : CommandHandlerPlugin
	{        
        static CASERibbon()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Utils.CurrentDomain_AssemblyResolve;
        }        

		/// <summary>
		/// Constructor, just initialises variables.
		/// </summary>
		public CASERibbon()
		{

        }

		public override int ExecuteCommand(string commandId, params string[] parameters)
		{            
			switch (commandId)
			{
				case "ID_arupissuetracker":
					{

						LaunchPlugin();
						break;
					}

				default:
					{
						MessageBox.Show("You have clicked on the command with ID = '" + commandId + "'");
						break;
					}
			}

			return 0;
		}

		public override bool TryShowCommandHelp(String commandId)
		{
			MessageBox.Show("Showing Help for command with the Id " + commandId);
			return true;
		}

		/// <summary>
		/// Launch
		/// </summary>
		public void LaunchPlugin()
		{

			// Running Navis
			if (Autodesk.Navisworks.Api.Application.IsAutomated)
			{
				throw new InvalidOperationException("Invalid when running using Automation");
            }

#if NAVIS2014
                string versionNumber = "2014";
#elif NAVIS2015
                string versionNumber = "2015";
#elif NAVIS2016
                string versionNumber = "2016";
#elif NAVIS2017
                string versionNumber = "2017";
#elif NAVIS2018
                string versionNumber = "2018";
#endif

            // Version
                if (!Autodesk.Navisworks.Api.Application.Version.RuntimeProductName.Contains(versionNumber))
			{
				MessageBox.Show(string.Format("Incompatible Navisworks Version" +
                                     "\nThis Add-In was built for Navisworks {0}, please go to the Arup Issue Tracker group in Yammer for assistance...", versionNumber),
									 "Cannot Continue!",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error);
				return;
			}

			//Find the plugin
            PluginRecord pr = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin("ARUP.IssueTracker.Navisworks.Plugin.CASE");

			if (pr != null && pr is DockPanePluginRecord && pr.IsEnabled)
			{                
                if (!File.Exists(Utils.issueTrackerDllPath))
				{
					MessageBox.Show("Required Issue Tracker Library Not Found");
					return;
				}

				// check if it needs loading
				if (pr.LoadedPlugin == null)
				{
					string exeConfigPath = GetType().Assembly.Location;
                    pr.LoadPlugin();
				}

				DockPanePlugin dpp = pr.LoadedPlugin as DockPanePlugin;
				if (dpp != null)
				{
					//switch the Visible flag
					dpp.Visible = !dpp.Visible;
				}
			}

		}

	}
}
