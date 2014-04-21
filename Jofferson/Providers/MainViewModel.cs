using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;
using Jofferson.Models;

namespace Jofferson
{
    class MainViewModel : INotifyPropertyChanged
    {
        // By doing this, I'm basically killing the MVVM idea
        public ICommand ShowResourceWindow { get; set; }
        public ICommand ShowErrorWindow { get; set; }
        public Providers.DelegateCommand<Resource> ShowJsonWindow { get; set; }

        public ObservableCollection<Views.ModListItem> Mods { get; private set; }
        public ObservableCollection<Views.ResourceListItem> Resources { get; private set; }
        public ObservableCollection<Views.ReferenceListItem> References { get; private set; }

        private Views.ModListItem currentMod;

        public Views.ModListItem CurrentModItem
        {
            get { return currentMod; }
            set { setCurrentMod(value); }
        }

        public Mod CurrentMod
        {
            get { return currentMod != null ? currentMod.Mod : null; }
            set { setCurrentMod(value); }
        }

        private Views.ResourceListItem currentResource;

        public Resource CurrentResource
        {
            get { return currentResource != null ? currentResource.Resource : null; }
        }

        public Views.ResourceListItem CurrentResourceItem {
            get { return currentResource; }
            set { setCurrentResource(value); }
        }

        public Reference CurrentReference
        {
            get { return currentReference != null ? currentReference.Reference : null; }
            //set { setCurrentReference(value); }
        }

        private Views.ReferenceListItem currentReference;

        public Views.ReferenceListItem CurrentReferenceItem
        {
            get { return currentReference; }
            set { setCurrentReference(value); }
        }

        public MainViewModel()
        {
            Mods = new ObservableCollection<Views.ModListItem>();
            Resources = new ObservableCollection<Views.ResourceListItem>();
            References = new ObservableCollection<Views.ReferenceListItem>();

            RefreshMods();
        }

        public void RefreshMods()
        {
            Mod currentMod = CurrentMod;
            Resource currentResource = CurrentResource;
            Reference currentReference = CurrentReference;

            CurrentMod = null;

            Mods.Clear();
            Resources.Clear();
            References.Clear();

            ResourceManager.Initialize();
            ResourceManager.LoadMods();
            ResourceManager.LoadManifests();
            ResourceManager.LoadMagic();
            ResourceManager.ProccessQueue();

            foreach (var mod in ResourceManager.Mods)
            {
                var modView = new Views.ModListItem(mod);
                this.Mods.Add(modView);
                if (currentMod != null && currentMod.Name == mod.Name)
                    setCurrentMod(modView);
            }

            if (currentResource != null)
            {
                foreach (var listItem in Resources)
                    if (listItem.Resource.Location == currentResource.Location)
                        setCurrentResource(listItem);
            }
        }

        private void setCurrentMod(Mod mod)
        {
            foreach (var listItem in Mods)
                if (listItem.Mod == mod)
                    setCurrentMod(listItem);
        }

        private void setCurrentMod(Views.ModListItem mod)
        {
            if (currentMod == mod)
                return;

            currentMod = mod;
            Resources.Clear();
            References.Clear();

            setCurrentResource(null);

            if (mod != null)
            {
                foreach (var e in mod.Mod.Resources.OrderBy(r => r.Valid).ThenBy(r => r.Location).Select(m => new Views.ResourceListItem(m, true)))
                {
                    Resources.Add(e);

                    if (e.Resource is Manifest)
                        setCurrentResource(e);
                }
            }

            RaisePropertyChanged("CurrentMod");
            RaisePropertyChanged("CurrentModItem");
        }

        private void setCurrentResource(Views.ResourceListItem resource)
        {
            if (currentResource == resource)
                return;

            currentResource = resource;

            if (resource != null)
            {
                if (resource.Resource.Mod != CurrentMod)
                    setCurrentMod(resource.Resource.Mod);

                References.Clear();
                // Get the references
                var references = resource.Resource.References.Select(m => new Views.ReferenceListItem(m, Views.ReferenceListItem.Display.NoMod, resource.Resource));
                var incomingReferences = resource.Resource.ReferredBy.Select(m => new Views.ReferenceListItem(m, Views.ReferenceListItem.Display.SourcedIncoming, resource.Resource));

                foreach (var e in references.Union(incomingReferences).OrderBy(r => r.Reference.Valid).ThenBy(r => r.ToString()))//resource.Resource.References.OrderBy(r => r.Valid).Select(m => new Views.ReferenceListItem(m, Views.ReferenceListItem.Display.NoMod, resource.Resource)))
                    References.Add(e);
            }

            RaisePropertyChanged("CurrentResource");
            RaisePropertyChanged("CurrentResourceItem");
            
            // Make sure that all resources are properly formatted
            Providers.Commands.OpenResource.RaiseCanExecuteChanged();
            Providers.Commands.OpenResourceFolder.RaiseCanExecuteChanged();
            ShowJsonWindow.RaiseCanExecuteChanged();
        }

        private void setCurrentReference(Views.ReferenceListItem reference)
        {
            if (currentReference == reference)
                return;

            currentReference = reference;
            RaisePropertyChanged("CurrentReference");
            RaisePropertyChanged("CurrentReferenceItem");
        }

        private void RaisePropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
