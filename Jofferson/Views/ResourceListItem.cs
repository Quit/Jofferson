using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Jofferson.Models;

namespace Jofferson.Views
{
    class ResourceListItem : FilterableListBoxItem
    {
        // TODO: Change the AliasListView to ResourceListView!
        public Resource Resource { get; private set; }

        public ResourceListItem(Resource resource, bool stripRoot)
        {
            this.Resource = resource;
            this.AddText(stripRoot ? Pather.GetWithoutRoot(resource.Location) : resource.Location);

            SetBackground(resource.Valid ? ItemStatus.Okay : ItemStatus.Error);

            this.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Copy,
                (o, e) => Clipboard.SetText(this.Resource.Location)
            ));

            this.ContextMenu = new ContextMenu();
            this.ContextMenu.Items.Add(new MenuItem { Header = "_Copy", Command = ApplicationCommands.Copy, CommandTarget = this });
            this.ContextMenu.Items.Add(new MenuItem { Header = "_Open in Explorer", Command = Providers.Commands.OpenResource, CommandTarget = this, CommandParameter = Resource });
            this.ContextMenu.Items.Add(new MenuItem { Header = "Open _folder in Explorer", Command = Providers.Commands.OpenResourceFolder, CommandTarget = this, CommandParameter = Resource });
        }

        public override string FilterString { get { return this.Resource.Location; } }
    }
}
