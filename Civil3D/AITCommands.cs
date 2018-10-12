// (C) Copyright 2017 by Microsoft 
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Windows;
using Autodesk.AutoCAD.GraphicsSystem;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(ARUP.IssueTracker.Civil3D.AITCommands))]

namespace ARUP.IssueTracker.Civil3D
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class AITCommands
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        private bool _isRunning;
        private Civil3DWindow window;

        // Modal Command with localized name
        [CommandMethod("AIT", "AITSNAPSHOT", "AITSNAPSHOT", CommandFlags.Modal)]
        public void GenerateSnapshot() // This method can have any name
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptResult pr = ed.GetString("\nOutput Path: ");
            if (pr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("No path was provided.\n");
                return;
            }

            var result = doc.Editor.SelectAll();
            if (result.Status == PromptStatus.OK)
            {
                Autodesk.AutoCAD.Internal.Utils.SelectObjects(result.Value.GetObjectIds());
            }
#if C3D2014
            doc.SendStringToExecute(string.Format("_.PNGOUT\n{0}\n\n\x03\x03", pr.StringResult), true, false, false);
#else
            doc.Editor.Command("_.PNGOUT", pr.StringResult, "");
#endif
        }

        /// <summary>
        /// Main Command Entry Point
        /// </summary>
        // Modal Command with localized name
        [CommandMethod("AIT", "AITRUN", "AITRUN", CommandFlags.Modal)]
        public void Run()
        {
            try
            {
#if C3D2014
                string versionNumber = "19.1";
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("FILEDIA", 0);
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
                    MessageBox.Show(string.Format("This Add-In was built for {0} {1}, please find the Arup Issue Tracker group in Yammer for assistance...", versionNumber, AITPlugin.getAutoCADProductName()), "Incompatible Version");
                    return;
                }

                // Form Running?
                if (_isRunning && window != null && window.IsLoaded)
                {
                    window.Focus();
                    return;
                }

                _isRunning = true;

                window = new Civil3DWindow(Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument);
                window.Show();

                // register a document closed event
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.BeginDocumentClose += MdiActiveDocument_BeginDocumentClose;

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("exception: " + ex);
            }

        }

        void MdiActiveDocument_BeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
        {
            window.Close();
        }

    }

}
