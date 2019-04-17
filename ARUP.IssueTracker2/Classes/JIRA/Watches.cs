using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARUP.IssueTracker.Classes.JIRA
{

    public class Watches
    {
        public string self { get; set; }
        public bool isWatching { get; set; }
        public int watchCount { get; set; }
        public List<Watcher> watchers { get; set; }
    }

    public class Watcher
    {
        public string self { get; set; }
        public string name
        {
            get { return accountId; }
            set { accountId = value; }
        }
        public string accountId { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
    }
}
