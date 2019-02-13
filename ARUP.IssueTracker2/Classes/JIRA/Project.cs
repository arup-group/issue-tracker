using System.Collections.Generic;
using System.Linq;

namespace ARUP.IssueTracker.Classes
{
    public class Project
    {
        public string self { get; set; }
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public Lead lead { get; set; }
        public AvatarUrls avatarUrls { get; set; }
        public List<Issuetype> issuetypes { get; set; }
        public string customGuidFieldName { get; set; }
        public string formattedname
        {
            get
            {
                return "[" + key + "] " + name;
            }
        }
        public string category 
        {
            get 
            {
                return ARUP.IssueTracker.UserControls.MainPanel.recentProjectKeys.Contains(key) ? "Recent Projects" : "All Other Projects";                
            }
        }
        public int recentProjectOrder 
        {
            get 
            {
                return ARUP.IssueTracker.UserControls.MainPanel.recentProjectKeys.IndexOf(key);
            }
        }
    }
}
