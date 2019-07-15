using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARUP.IssueTracker.UserControls;
using Arup.RestSharp;
using System.Net;

namespace ARUP.IssueTracker.Classes
{
    public static class JiraClient
    {
        static JiraClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
        private static RestClient client;
        public static RestClient Client
        {
            get
            {
                return client;
            }

            set
            {
                client = value;
            }
        }

        public static Waiter Waiter;
    }
}
