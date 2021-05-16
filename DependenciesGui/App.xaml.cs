using System;
using System.Windows;
using System.Windows.Shell;
using System.ComponentModel;
using System.IO;

using Dependencies.ClrPh;

namespace Dependencies
{
 

    /// <summary>
    /// Application instance
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        private string statusBarMessage = "";
        private MainWindow mainWindow;

        public string StatusBarMessage
        {
            get { return statusBarMessage; }
            set
            {
                if (statusBarMessage != value)
                {
                    statusBarMessage = value;
                    OnPropertyChanged("StatusBarMessage");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void App_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "StatusBarMessage")
            {
                mainWindow.AppStatusBarMessage.Content = (object)StatusBarMessage;
            }
        }

        public PE LoadBinary(string path)
        {
            StatusBarMessage = String.Format("Loading module {0:s} ...", path);

			if (!NativeFile.Exists(path))
			{
				StatusBarMessage = String.Format("Loading PE file \"{0:s}\" failed : file not present on disk.", path);
				return null;
			}

			PE pe = BinaryCache.LoadPe(path);
            if (pe == null || !pe.LoadSuccessful)
            {
                StatusBarMessage = String.Format("Loading module {0:s} failed.", path);
            }
            else
            {
                StatusBarMessage = String.Format("Loading PE file \"{0:s}\" successful.", pe.Filepath);
            }
            
            return pe;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            (Application.Current as App).PropertyChanged += App_PropertyChanged;

            Phlib.InitializePhLib();

            // Load singleton for binary caching
            if (Dependencies.BinaryCacheOption.GetGlobalBehaviour() == Dependencies.BinaryCacheOption.BinaryCacheOptionValue.Yes)
            {
                string ApplicationLocalAppDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Dependencies"
                );
                BinaryCache.Instance = new BinaryCacheImpl(ApplicationLocalAppDataPath, 200);
            }
            else
            {
                BinaryCache.Instance = new BinaryNoCacheImpl();
            }
            
            BinaryCache.Instance.Load();

            mainWindow = new MainWindow();
            mainWindow.IsMaster = true;

            switch(Phlib.GetClrPhArch())
            {
                case CLRPH_ARCH.x86:
                    mainWindow.Title = "Dependencies (x86)";
                    break;
                case CLRPH_ARCH.x64:
                    mainWindow.Title = "Dependencies (x64)";
                    break;
                case CLRPH_ARCH.WOW64:
                    mainWindow.Title = "Dependencies (WoW64)";
                    break;
            }
            
            mainWindow.Show();
            

            // Process command line args
            if (e.Args.Length > 0)
            {
                mainWindow.OpenNewDependencyWindow(e.Args[0]);
                
            }
        }

        void App_Exit(object sender, ExitEventArgs e)
        {
            Dependencies.Properties.Settings.Default.Save();
            BinaryCache.Instance.Unload();
        }

        public static void AddToRecentDocuments(String Filename)
        {
            // Create custom task
            JumpTask item = new JumpTask();
            item.Title = System.IO.Path.GetFileName(Filename);
            item.Description = Filename;
            item.ApplicationPath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            item.Arguments = Filename;
            item.CustomCategory = "Tasks";
            

            // Add document to recent category
            JumpList RecentsDocs = JumpList.GetJumpList(Application.Current);
            RecentsDocs.JumpItems.Add(item);
            JumpList.AddToRecentCategory(item);
            RecentsDocs.Apply();

            // Store a copy in application settings, LRU style
            // First check if the item is not already present in the list
            int index = Dependencies.Properties.Settings.Default.RecentFiles.IndexOf(Filename);
            if (index != -1)
            {
                Dependencies.Properties.Settings.Default.RecentFiles.RemoveAt(index);
            }

            // Second check if the list is not full
            if (Dependencies.Properties.Settings.Default.RecentFiles.Count == 10)
            {
                Dependencies.Properties.Settings.Default.RecentFiles.RemoveAt(9);
            }

            // Prepend the list with the new item
            Dependencies.Properties.Settings.Default.RecentFiles.Insert(0, Filename);
        }

    }
    
}
