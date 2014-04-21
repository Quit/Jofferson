using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Jofferson.Views
{
    enum ItemStatus
    {
        Okay,
        Ignored,
        Warning,
        Error
    }

    abstract class FilterableListBoxItem : ListBoxItem, Helpers.IFilterable
    {
        public abstract string FilterString { get; }

        //public new MainViewModel DataContext { get { return (MainViewModel)base.DataContext; } }

        public FilterableListBoxItem()
        {
        }

        /// <summary>
        /// TODO: Move this to... XML?
        /// </summary>
        private static readonly Dictionary<ItemStatus, Brush> Brushes = new Dictionary<ItemStatus, Brush>
        {
            { ItemStatus.Okay, new SolidColorBrush(Color.FromRgb(96, 255, 131)) },
            { ItemStatus.Ignored, new SolidColorBrush(Color.FromRgb(200, 200, 200)) },
            { ItemStatus.Error, new SolidColorBrush(Color.FromRgb(255, 112, 112)) },
            { ItemStatus.Warning, new SolidColorBrush(Color.FromRgb(255, 224, 131)) }
        };

        protected void SetBackground(ItemStatus status)
        {
            this.Background = Brushes[status];
        }
    }
}
