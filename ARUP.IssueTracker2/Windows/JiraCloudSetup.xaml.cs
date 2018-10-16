using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ARUP.IssueTracker.Windows
{
    /// <summary>
    /// Interaction logic for JiraCloudSetup.xaml
    /// </summary>
    public partial class JiraCloudSetup : Window
    {
        private readonly string tokenGenerationUrl = "https://id.atlassian.com/manage/api-tokens";
        private readonly string authScriptUrl = "http://to.arup.sg/aitjiracloudauth";
        public System.Windows.Forms.Integration.WindowsFormsHost host;
        public System.Windows.Forms.WebBrowser webBrowser;
        private string authScriptText;
        public string email;
        public string apiToken;

        public JiraCloudSetup()
        {
            InitializeComponent();

            // initialize web browser control
            webBrowser = new System.Windows.Forms.WebBrowser();
            ObjectForScriptingHelper helper = new ObjectForScriptingHelper(this);
            webBrowser.ObjectForScripting = helper;
            webBrowser.AllowWebBrowserDrop = false;
            webBrowser.IsWebBrowserContextMenuEnabled = false;
            webBrowser.WebBrowserShortcutsEnabled = false;
            webBrowser.DocumentCompleted += webBrowser_DocumentCompleted;

            // add to grid
            host = new System.Windows.Forms.Integration.WindowsFormsHost();
            host.Child = webBrowser;
            container.Children.Add(host);

            // download auth script from Github Gist
            try 
            {
                using (var wc = new System.Net.WebClient())
                {
                    authScriptText = wc.DownloadString("");
                }
            }
            catch(Exception ex)
            {
                authScriptText = @"

                    window.onload = function() {
                        if(window.location.href.startsWith != undefined && !window.location.href.startsWith('https://id.atlassian.com/login')){                        
                            if(window.location.href.startsWith('https://id.atlassian.com/manage/api-tokens')){
                                window.external.hideControls();
                                var allScripts = document.getElementsByTagName('script');
                                var scriptText = allScripts[allScripts.length-1].innerText;
                                var indexOfCSRF = scriptText.indexOf('csrfToken');
                                var indexOfEnd = scriptText.indexOf(',', indexOfCSRF);
                                var csrfToken = scriptText.substring(indexOfCSRF+9, indexOfEnd).replace(/\W/g, '');
                                console.log(csrfToken);
                                var indexOfEmail = scriptText.indexOf('""email"":');
                                var indexOfEnd = scriptText.indexOf('"",""', indexOfEmail);
                                var email = scriptText.substring(indexOfEmail+9, indexOfEnd);
                                console.log(email);
                                var createTokenRequest = {
                                    'url': 'https://id.atlassian.com/manage/rest/api-tokens',
                                    'method': 'POST',
                                    'headers': {
                                      'content-type': 'application/json',
                                      'cache-control': 'no-cache',
                                      'x-csrf-token': csrfToken 
                                    },
                                    'processData': false,
                                    'data': JSON.stringify({label: 'API_TOKEN_NAME'}) // to be replaced by timestamp
                                };
                                $.ajax(createTokenRequest).done(function (response) {
                                    console.log(response.passwordValue);                                
                                    window.external.setCredentials(email, response.passwordValue);
                                }).fail(function (jqXHR, textStatus) {
                                    console.log('Failed.');                                
                                }).always(function (jqXHR, textStatus) {
                                    window.location.href = 'https://id.atlassian.com/logout';
                                });
                            }
                        }else{
                            window.external.closeWindow();
                        }
                    };
                
                ";
            }

            // go to client app url
            webBrowser.Navigate(new Uri(tokenGenerationUrl, UriKind.RelativeOrAbsolute));
        }

        public void webBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {
            webBrowser.Document.InvokeScript("eval", new object[] { authScriptText.Replace("API_TOKEN_NAME", string.Format("Arup_Issue_Tracker_{0}", DateTime.Now.ToString())) });
        }
    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ObjectForScriptingHelper
    {
        JiraCloudSetup window;
        public ObjectForScriptingHelper(JiraCloudSetup window)
        {
            this.window = window;
        }

        public void setCredentials(string email, string apiToken)
        {
            window.DialogResult = true;
            window.email = email;
            window.apiToken = apiToken;
        }

        public void hideControls()
        {
            window.host.Visibility = Visibility.Hidden;
        }

        public void closeWindow()
        {
            if (window.host.Visibility == Visibility.Hidden)
            {
                window.Close();
            }
        }
    }
}
