using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Jofferson.Models;

namespace Jofferson.Views
{
    class ReferenceListItem : FilterableListBoxItem
    {
        public enum Display
        {
            NoMod,
            WithMod,
            SourcedNoMod,
            SourcedIncoming
        }

        public Reference Reference { get; private set; }

        // TODO: Find a *much* nicer way of dealing with this. :X
        public ReferenceListItem(Reference reference, Display display, Resource context)
        {
            this.Reference = reference;

            string displayStr;

            switch (display)
            {
                case Display.NoMod:
                    displayStr = reference.ToString(false, context);
                    break;
                case Display.WithMod:
                    displayStr = reference.ToString(true, context);
                    break;
                case Display.SourcedNoMod:
                    displayStr = reference.ToStringSourced(false, context);
                    break;
                case Display.SourcedIncoming:
                    displayStr = "[INCOMING] " + reference.ToStringSourced(false, context);
                    break;
                default:
                    throw new ArgumentException("display");
            }

            this.AddText(displayStr);

            SetBackground(reference.Valid ? ItemStatus.Okay : ItemStatus.Error);


            this.CommandBindings.Add(new System.Windows.Input.CommandBinding(
                System.Windows.Input.ApplicationCommands.Copy,
                (o, e) => System.Windows.Clipboard.SetText(string.Concat(reference.Origin.Location, " : ", reference.Target.Location))
                ));

            this.ContextMenu = new ContextMenu();

            var items = this.ContextMenu.Items;

            items.Add(new MenuItem { Header = "_Copy...", Command = System.Windows.Input.ApplicationCommands.Copy, CommandTarget = this });

            items.Add(CreateItem(Reference.Definition, "_Definition: "));
            // Add the origin unless it's the same
            if (Reference.Definition != reference.Origin)
                items.Add(CreateItem(Reference.Origin, "_Origin: "));
            // Add the definition *unless* it's either.
            if (Reference.Definition != Reference.Target && Reference.Origin != Reference.Target)
                items.Add(CreateItem(Reference.Target, "_Target: "));
        }

        private static MenuItem CreateItem(Resource resource, string prefix)
        {
            var item = new MenuItem { Header = prefix + resource.Location.Replace("_", "__") };
            item.Items.Add(new MenuItem { Header = "_Copy location...", Command = Providers.Commands.CopyResource, CommandParameter = resource });
            item.Items.Add(new MenuItem { Header = "_Open file...", Command = Providers.Commands.OpenResource, CommandParameter = resource });
            item.Items.Add(new MenuItem { Header = "Open file _folder...", Command = Providers.Commands.OpenResourceFolder, CommandParameter = resource });
            return item;
        }
        public override string FilterString { get { return this.Reference.ToString(false, null); } }
    }
}
