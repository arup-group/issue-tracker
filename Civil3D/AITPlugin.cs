// (C) Copyright 2017 by Microsoft 
//
using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Reflection;
using System.Windows.Media.Imaging;

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(ARUP.IssueTracker.Civil3D.AITPlugin))]

namespace ARUP.IssueTracker.Civil3D
{

    // This class is instantiated by AutoCAD once and kept alive for the 
    // duration of the session. If you don't do any one time initialization 
    // then you should remove this class.
    public class AITPlugin : IExtensionApplication
    {
        private string _path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        #region IExtensionApplication Implementation

        /// <summary>
        /// Startup
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        void IExtensionApplication.Initialize()
        {
            try
            {
#if C3D2014
                string versionNumber = "19.1";
#elif C3D2015
                string versionNumber = "20.0";
#elif C3D2016
                string versionNumber = "20.1";
#elif C3D2017
                string versionNumber = "21.0";
#elif C3D2018
                string versionNumber = "22.0";
#elif C3D2019
                string versionNumber = "23.0";
#endif

                // Version
                if (!Autodesk.AutoCAD.ApplicationServices.Application.Version.ToString().Contains(versionNumber))
                {
                    MessageBox.Show(string.Format("This Add-In was built for {0} {1}, please find the Arup Issue Tracker group in Yammer for assistance...", versionNumber, getAutoCADProductName()), "Incompatible Version");
                }

                // Assembly Paths
                string m_issuetracker = Path.Combine(ProgramFilesx86(), "CASE", "ARUP Issue Tracker", "ARUP.IssueTracker.dll");

                // Check that File Exists
                if (!File.Exists(m_issuetracker))
                {
                    MessageBox.Show(m_issuetracker, "Required Issue Tracker Library Not Found");
                }

                // Load Assemblies
                Assembly.LoadFrom(m_issuetracker);

                // Initiate RestSharp first to avoid the conflict with Dynamo
                // string restSharpDllPath = Path.Combine(ProgramFilesx86(), "CASE", "ARUP Issue Tracker", "RestSharp.dll");
                // Assembly.LoadFrom(restSharpDllPath);

            }
            catch (System.Exception ex1)
            {
                MessageBox.Show("exception: " + ex1);
            }
        }

        void IExtensionApplication.Terminate()
        {
            // Do plug-in application clean up here
        }

        #endregion

        public static string getAutoCADProductName()
        {
            string windowTitle = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Text;
            var tokens = windowTitle.Split(' ').ToList();
            int startTokenIndex = -1, endTokenIndex = -1;
            for(int i=0; i<tokens.Count; i++)
            {
                int version;
                if(tokens[i] == "Autodesk")
                {
                    startTokenIndex = i;
                }
                else if(int.TryParse(tokens[i], out version) && tokens[i].StartsWith("20"))
                {
                    endTokenIndex = i;
                }
            }

            return string.Join(" ", tokens.GetRange(startTokenIndex, endTokenIndex-startTokenIndex));
        }

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
