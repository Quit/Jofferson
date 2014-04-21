using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Jofferson.Models;

namespace Jofferson.Providers
{
    class ResourceViewModel : INotifyPropertyChanged
    {
        public Resource Resource { get; private set; }

        public ICollection<Views.ReferenceListItem> References { get; private set; }
        public ICollection<Views.ReferenceListItem> ReferredBy { get; private set; }
        public ICommand OpenJsonWindowCommand { get; private set; }

        public ResourceViewModel(Resource resource, ICommand openJsonWindowCommand)
        {
            this.Resource = resource;
            this.References = resource.References.Select(m => new Views.ReferenceListItem(m, Views.ReferenceListItem.Display.NoMod, resource)).ToList();
            this.ReferredBy = resource.ReferredBy.Select(m => new Views.ReferenceListItem(m, Views.ReferenceListItem.Display.SourcedNoMod, resource)).ToList();
            this.OpenJsonWindowCommand = openJsonWindowCommand;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
