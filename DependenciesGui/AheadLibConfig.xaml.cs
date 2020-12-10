using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

namespace Dependencies
{
    /// <summary>
    /// AheadLibConfig.xaml 的交互逻辑
    /// </summary>
    public partial class AheadLibConfig : Window,INotifyPropertyChanged
    {
        private string _CodeGenPath;
        public string CodeGenPath 
        { 
            get { return _CodeGenPath; }
            set { _CodeGenPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CodeGenPath"));} 
        }


        private string _OldDllFullName="test.dll.old";
        public string OldDllFullName
        {
            get { return _OldDllFullName; }
            set { _OldDllFullName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OldDllFullName")); }
        }


        private bool _IsCodegenFunctionTrace=true;
        public bool IsCodegenFunctionTrace
        {
            get { return _IsCodegenFunctionTrace; }
            set { _IsCodegenFunctionTrace = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsCodegenFunctionTrace")); }
        }
        private CodeGenDllMode _CodeGenDllMode =  CodeGenDllMode.FileMode;
        public CodeGenDllMode CodeGenDllMode
        {
            get { return _CodeGenDllMode; }
            set { _CodeGenDllMode = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CodeGenDllMode")); }
        }

        private string _LogPath="D:/";

        public string LogPath
        {
            get { return _LogPath; }
            set { _LogPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LogPath")); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private MessageBoxResult _ShowResult;

        public AheadLibConfig()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public new MessageBoxResult ShowDialog()
        {
            base.ShowDialog();       
            return _ShowResult;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(CodeGenPath)==false)
            {
                MessageBox.Show("CodeGenPath is not Exist!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if(IsCodegenFunctionTrace==true&&Directory.Exists(LogPath)==false)
            {
                MessageBox.Show("LogPath is not Exist!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _ShowResult = MessageBoxResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _ShowResult = MessageBoxResult.Cancel;
            this.Close();
        }

        private void CodeGenBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "please codegen path";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CodeGenPath = dialog.FileName;
            }
        }

        private void LogPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "please codegen path";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LogPath = dialog.FileName;
            }
        }
    }
}
