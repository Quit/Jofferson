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
using System.Windows.Shapes;

namespace Jofferson
{
    /// <summary>
    /// Interaction logic for ResourceInfoWindow.xaml
    /// </summary>
    public partial class ResourceInfoWindow : Window
    {
        public ICommand ShowResourceWindow { get; set; }

        public ResourceInfoWindow()
        {
            InitializeComponent();
        }

        private void OpenOutgoing(object sender, MouseButtonEventArgs e)
        {
            Views.ReferenceListItem item = (Views.ReferenceListItem)((ListBox)sender).SelectedItem;
            if (item == null)
                return;

            ShowResourceWindow.Execute(item.Reference.Target);
        }

        private void OpenIncoming(object sender, MouseButtonEventArgs e)
        {
            Views.ReferenceListItem item = (Views.ReferenceListItem)((ListBox)sender).SelectedItem;
            if (item == null)
                return;
            ShowResourceWindow.Execute(item.Reference.Definition);
        }
    }
}
