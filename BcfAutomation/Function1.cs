using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ARUP.IssueTracker.Classes;
using ARUP.IssueTracker.Classes.BCF2;
using Ionic.Zip;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace BcfAutomation
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run(
            [QueueTrigger("bcf")]QueueMessage myQueueItem,
            [Blob("bcf/{blobName}", FileAccess.Read)] Stream myBlob,
            ILogger log)
        {
            log.LogInformation($"Jira Address: {myQueueItem.jiraAddress}");
            log.LogInformation($"Jira Project Key: {myQueueItem.jiraProjectKey}");
            log.LogInformation($"Blob Name: {myQueueItem.blobName}");
            log.LogInformation($"Blob size: {myBlob.Length}");

            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            using (ZipFile zip = ZipFile.Read(myBlob))
            {
                zip.ExtractAll(tempFolder);
            }

            var dir = new DirectoryInfo(tempFolder);

            // Check BCF version
            bool isBCF2 = false;
            foreach (var file in dir.GetFiles())
            {
                if (File.Exists(Path.Combine(dir.FullName, "bcf.version")))
                {
                    // This is a BCF 2.0 file
                    isBCF2 = true;
                }
            }

            List<Markup> issues = new List<Markup>();

            int errorCount = 0;
            //ADD ISSUES FOR EACH SUBFOLDER
            foreach (var folder in dir.GetDirectories())
            {
                //BCF ISSUE is not complete
                if (!File.Exists(Path.Combine(folder.FullName, "snapshot.png")) || !File.Exists(Path.Combine(folder.FullName, "markup.bcf")) || !File.Exists(Path.Combine(folder.FullName, "viewpoint.bcfv")))
                {
                    errorCount++;
                    continue;
                }

                // This is a BCF 2.0 issue object
                Markup i = null;
                FileStream viewpointFile = new FileStream(Path.Combine(folder.FullName, "viewpoint.bcfv"), FileMode.Open);
                FileStream markupFile = new FileStream(Path.Combine(folder.FullName, "markup.bcf"), FileMode.Open);

                // all other viewpoints and snapshots
                List<string> otherViewpointFiles = new List<string>();
                List<string> otherSnapshotFiles = new List<string>();
                foreach (var file in folder.GetFiles())
                {
                    if (file.Name != "viewpoint.bcfv" && file.Extension == ".bcfv")
                    {
                        otherViewpointFiles.Add(file.Name);
                    }
                    else if (file.Name != "snapshot.png" && (file.Extension == ".png" || file.Extension == ".jpg" || file.Extension == ".jpeg" || file.Extension == ".bmp"))
                    {
                        otherSnapshotFiles.Add(file.Name);
                    }
                }

                if (isBCF2)
                {
                    XmlSerializer serializerS = new XmlSerializer(typeof(ARUP.IssueTracker.Classes.BCF2.VisualizationInfo));
                    ARUP.IssueTracker.Classes.BCF2.VisualizationInfo viewpoint = serializerS.Deserialize(viewpointFile) as ARUP.IssueTracker.Classes.BCF2.VisualizationInfo;

                    XmlSerializer serializerM = new XmlSerializer(typeof(ARUP.IssueTracker.Classes.BCF2.Markup));
                    ARUP.IssueTracker.Classes.BCF2.Markup markup = serializerM.Deserialize(markupFile) as ARUP.IssueTracker.Classes.BCF2.Markup;

                    if (markup != null && viewpoint != null)
                    {
                        i = markup;
                        foreach (var v in i.Viewpoints)
                        {
                            // handle viewpoint file
                            if (v.Viewpoint == "viewpoint.bcfv")
                            {
                                v.VisInfo = viewpoint;
                            }
                            else if (otherViewpointFiles.Contains(v.Viewpoint))
                            {
                                using (FileStream vFile = new FileStream(Path.Combine(folder.FullName, v.Viewpoint), FileMode.Open))
                                {
                                    ARUP.IssueTracker.Classes.BCF2.VisualizationInfo vi = serializerS.Deserialize(vFile) as ARUP.IssueTracker.Classes.BCF2.VisualizationInfo;
                                    if (vi != null)
                                    {
                                        v.VisInfo = vi;
                                    }
                                }
                            }
                            // add reference to comment
                            foreach (var comm in i.Comment)
                            {
                                if (comm.Viewpoint != null)
                                {
                                    if (comm.Viewpoint.Guid == v.Guid)
                                    {
                                        comm.visInfo = v.VisInfo;
                                    }
                                }
                            }

                            // handle snapshot file
                            if (v.Snapshot == "snapshot.png")
                            {
                                v.SnapshotPath = Path.Combine(folder.FullName, "snapshot.png");
                            }
                            else if (otherSnapshotFiles.Contains(v.Snapshot))
                            {
                                v.SnapshotPath = Path.Combine(folder.FullName, v.Snapshot);
                            }
                            // add reference to comment
                            foreach (var comm in i.Comment)
                            {
                                if (comm.Viewpoint != null)
                                {
                                    if (comm.Viewpoint.Guid == v.Guid)
                                    {
                                        comm.snapshotFullUrl = v.SnapshotPath;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ARUP.IssueTracker.Classes.BCF1.IssueBCF bcf1Issue = new ARUP.IssueTracker.Classes.BCF1.IssueBCF();
                    bcf1Issue.guid = new Guid(folder.Name);  // need to overwrite the guid generated by default constructor
                    bcf1Issue.snapshot = Path.Combine(folder.FullName, "snapshot.png");

                    XmlSerializer serializerS = new XmlSerializer(typeof(ARUP.IssueTracker.Classes.BCF1.VisualizationInfo));
                    bcf1Issue.viewpoint = serializerS.Deserialize(viewpointFile) as ARUP.IssueTracker.Classes.BCF1.VisualizationInfo;

                    XmlSerializer serializerM = new XmlSerializer(typeof(ARUP.IssueTracker.Classes.BCF1.Markup));
                    bcf1Issue.markup = serializerM.Deserialize(markupFile) as ARUP.IssueTracker.Classes.BCF1.Markup;
                    if (bcf1Issue.markup.Comment != null)
                        bcf1Issue.markup.Comment = new ObservableCollection<ARUP.IssueTracker.Classes.BCF1.CommentBCF>(bcf1Issue.markup.Comment.OrderByDescending(o => o.Date));

                    i = BcfAdapter.LoadBcf2IssueFromBcf1(bcf1Issue);
                }

                viewpointFile.Close();
                markupFile.Close();

                if (i != null)
                    issues.Add(i);
            }

            log.LogInformation($"Number of issues: {issues.Count}");
        }
    }

    public class QueueMessage
    {
        public string jiraAddress { get; set; }
        public string jiraProjectKey { get; set; }
        public string blobName { get; set; }
    }
}
