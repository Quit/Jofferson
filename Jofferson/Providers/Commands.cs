using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using Jofferson.Models;

namespace Jofferson.Providers
{
    static class Commands
    {
        public static DelegateCommand<Resource> OpenResource { get; private set; }
        public static DelegateCommand<Resource> OpenResourceFolder { get; private set; }
        public static DelegateCommand<Resource> CopyResource { get; private set; }
        
        static Commands()
        {
            OpenResource = new DelegateCommand<Resource>(o => OpenInExplorer(o.FullLocation), CanOpen);
            OpenResourceFolder = new DelegateCommand<Resource>(o => OpenInExplorer(new FileInfo(o.FullLocation).Directory.FullName), CanOpen);
            CopyResource = new DelegateCommand<Resource>(o => System.Windows.Clipboard.SetText(o.Location), o => true);
        }

        public static void RefreshCommands()
        {
            OpenResource.RaiseCanExecuteChanged();
            OpenResourceFolder.RaiseCanExecuteChanged();
        }

        private static void OpenInExplorer(string path)
        {
            Process.Start(path);
        }

        private static bool CanOpen(Resource resource)
        {
            if (resource == null)
                return true;

            return resource.Mod.FileSystem.IsAccessible && resource.Exists;
        }
    }
}
