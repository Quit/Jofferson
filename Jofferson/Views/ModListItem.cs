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
    class ModListItem : FilterableListBoxItem
    {
        public Mod Mod { get; private set; }

        public ModListItem(Mod mod)
        {
            this.Mod = mod;
            this.AddText(mod.Name);

            if (!mod.Valid)
                SetBackground(ItemStatus.Error);
            else
            {
                if (mod.Empty)
                    SetBackground(ItemStatus.Ignored);
                else
                    SetBackground(ItemStatus.Okay);
            }

            this.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Copy,
                (o, e) => Clipboard.SetText(this.Mod.Name)
            ));

            this.ContextMenu = new ContextMenu();
            this.ContextMenu.Items.Add(new MenuItem { Header = "Copy Name", Command = ApplicationCommands.Copy, CommandTarget = this });
        }

        public override string FilterString { get { return this.Mod.Name; } }

        protected override void OnSelected(RoutedEventArgs e)
        {
            if (this.DataContext != null)
                ((MainViewModel)this.DataContext).CurrentMod = this.Mod;
            base.OnSelected(e);
        }

        public bool Valid { get { return Mod.Valid; } }
    }
}