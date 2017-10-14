using System;
using System.Windows;
using System.Reflection;


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

            this.TextContent.Text = String.Format("Dependencies v{0:s} :\n\nDependency tool made by lucasg@github.com.\nPlease go to \"https://github.com/lucasg/Dependencies/issues\" for filing issues", Assembly.GetEntryAssembly().GetName().Version);
        }
    }
}
