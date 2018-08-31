using System.Diagnostics;
using System.Reflection;
using System.Windows;
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
            Process.Start(e.Uri.ToString());
        }

        public string VersionStr
        {
            get
            {
                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }
    }
}
