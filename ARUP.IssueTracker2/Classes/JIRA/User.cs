namespace ARUP.IssueTracker.Classes
{
    public class User
    {
        public string self { get; set; }
        public string key
        {
            get { return accountId; }
            set { accountId = value; }
        }
        public string name 
        {
            get { return accountId; }
            set { accountId = value; } 
        }
        public string accountId { get; set; }
        public string emailAddress { get; set; }
        public AvatarUrls avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
    }
}
