using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Dependencies
{
    /// <summary>
    /// Logique d'interaction pour About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Uri_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.ToString(), UseShellExecute = true });
        }

        public string VersionStr
        {
            get
            {
                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }

        private async void Uri_CheckUpdates(object sender, RequestNavigateEventArgs e)
        {
            string version = await GetLatestVersion("https://github.com/lucasg/Dependencies/releases/latest");
            UpdateCheck.Inlines.Clear();
            UpdateCheck.Inlines.Add("Latest version: ");
            var link = new Hyperlink()
            {
                NavigateUri = new Uri(version)
            };
            link.Inlines.Add(version);
            link.RequestNavigate += Uri_RequestNavigate;
            UpdateCheck.Inlines.Add(link);
        }

        // Based on https://stackoverflow.com/a/28424940/4928207
        private async Task<string> GetLatestVersion(string url)
        {
            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Method = "HEAD";
                req.AllowAutoRedirect = false;
                using (var resp = (HttpWebResponse)await req.GetResponseAsync())
                {
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return url;
                        case HttpStatusCode.Redirect:
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            if (resp.Headers["Location"] == null)
                                return url;
                            return resp.Headers["Location"];
                        default:
                            return url;
                    }
                }
            }
            catch
            {
            }
            return url;
        }
    }
}
