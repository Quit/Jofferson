using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Jofferson.Views;
using Jofferson.Models;

namespace Jofferson
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ICommand OpenResourceWindowCommand;
        private ICommand OpenErrorWindowCommand;
        private Providers.DelegateCommand<Resource> OpenJsonWindowCommand;

        private Dictionary<Resource, ResourceInfoWindow> ResourceWindows = new Dictionary<Resource, ResourceInfoWindow>();
        private Dictionary<Mod, ErrorWindow> ErrorWindows = new Dictionary<Mod, ErrorWindow>();
        private Dictionary<Resource, JsonWindow> JsonWindows = new Dictionary<Resource, JsonWindow>();

        private FileSystemWatcher watcher;
        private DateTime lastRefresh = DateTime.MinValue;
        private bool promptedRefreshRequest = false;

        private MainViewModel mainViewModel;

        public MainWindow()
        {
            // Initialize WPF or something
            InitializeComponent();

            this.OpenResourceWindowCommand = new Providers.DelegateCommand<Resource>(r => OpenResourceWindow(r), r => true);
            this.OpenErrorWindowCommand = new Providers.DelegateCommand<Mod>(r => OpenErrorWindow(r), r => true);
            this.OpenJsonWindowCommand = new Providers.DelegateCommand<Resource>(r => OpenJsonWindow(r), r => r is JsonResource && r.Exists);

            mainViewModel = new MainViewModel();
            mainViewModel.ShowResourceWindow = OpenResourceWindowCommand;
            mainViewModel.ShowErrorWindow = OpenErrorWindowCommand;
            mainViewModel.ShowJsonWindow = OpenJsonWindowCommand;
            this.DataContext = mainViewModel;

            // Set up the filters
            new Helpers.TextFilter(CollectionViewSource.GetDefaultView(mainViewModel.Mods), this.ModFilterInput);
            new Helpers.TextFilter(CollectionViewSource.GetDefaultView(mainViewModel.Resources), this.ResourceFilterInput);
            new Helpers.TextFilter(CollectionViewSource.GetDefaultView(mainViewModel.References), this.ReferenceFilterInput);


            watcher = new System.IO.FileSystemWatcher(Environment.CurrentDirectory, "*.json");
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            watcher.Renamed += onFileChange;
            watcher.Deleted += onFileChange;
            watcher.Changed += onFileChange;
            watcher.Changed += onFileChange;
            watcher.EnableRaisingEvents = true;
        }

        private void onFileChange(object sender, FileSystemEventArgs args)
        {
            DateTime now = DateTime.Now;
            this.Dispatcher.InvokeAsync(delegate { fileChanged(args.FullPath, now); });
        }

        private void fileChanged(string filename, DateTime committed)
        {
            if (lastRefresh >= committed || promptedRefreshRequest)
                return;

            promptedRefreshRequest = true;
            if (MessageBox.Show(this, string.Format("{0} has been modified.\nReload all mods?", filename), "File has been modified", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                mainViewModel.RefreshMods();
                foreach (var p in this.OwnedWindows)
                    ((Window)p).Close();

                ErrorWindows.Clear();
                ResourceWindows.Clear();
                JsonWindows.Clear();
            }

            promptedRefreshRequest = false;
            
            lastRefresh = DateTime.Now;
        }

        private void OpenResourceWindow(Resource resource)
        {
            
            ResourceInfoWindow window;
            if (ResourceWindows.TryGetValue(resource, out window))
            {
                window.Focus();
                return;
            }

            window = new ResourceInfoWindow();
            window.Owner = this;
            window.ShowResourceWindow = OpenResourceWindowCommand;
            window.DataContext = new Providers.ResourceViewModel(resource, OpenJsonWindowCommand);
            window.Closed += delegate { ResourceWindows.Remove(resource); };
            window.Show();
            ResourceWindows.Add(resource, window);
            Providers.Commands.RefreshCommands();
            OpenJsonWindowCommand.RaiseCanExecuteChanged();
        }

        private void OpenErrorWindow(Mod mod)
        {
            ErrorWindow window;
            if (ErrorWindows.TryGetValue(mod, out window))
            {
                window.Focus();
                return;
            }

            window = new ErrorWindow();
            window.Owner = this;
            //window.ShowResourceWindow = OpenResourceWindowCommand;
            window.DataContext = mod;// new Providers.ResourceViewModel(mod);
            window.Closed += delegate { ErrorWindows.Remove(mod); };
            window.Show();
            ErrorWindows.Add(mod, window);
        }

        private void OpenJsonWindow(Resource res)
        {
            JsonWindow window;
            if (JsonWindows.TryGetValue(res, out window))
            {
                window.Focus();
                return;
            }

            window = new JsonWindow();
            window.Owner = this;
            //window.ShowResourceWindow = OpenResourceWindowCommand;
            window.DataContext = res;// new Providers.ResourceViewModel(mod);
            window.Closed += delegate { JsonWindows.Remove(res); };
            window.Show();
            JsonWindows.Add(res, window);
        }

        private void ResourceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ResourceListItem item = (ResourceListItem)((ListBox)sender).SelectedItem;

            if (item == null)
                return;

            OpenResourceWindow(item.Resource);
        }

        private void ReferenceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ReferenceListItem item = (ReferenceListItem)((ListBox)sender).SelectedItem;

            if (item == null)
                return;

            OpenResourceWindow(item.Reference.Target);
        }
    }
}
