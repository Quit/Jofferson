using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Jofferson.Helpers
{
    interface IFilterable
    {
        string FilterString { get; }
    }

    public class TextFilter
    {
        public TextFilter(ICollectionView filteredView, TextBox box)
        {
            string filterText = "";

            filteredView.Filter = delegate(object obj)
            {
                if (string.IsNullOrEmpty(filterText))
                    return true;

                string str = obj as string;

                if (obj is IFilterable)
                    str = ((IFilterable)obj).FilterString;

                if (string.IsNullOrEmpty(str))
                    return false;


                return str.IndexOf(filterText, 0, StringComparison.InvariantCultureIgnoreCase) >= 0;
            };

            box.TextChanged += delegate { 
                filterText = box.Text; 
                filteredView.Refresh(); 
            };
        }
    }
}
