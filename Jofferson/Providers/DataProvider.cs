using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jofferson.Providers
{
    [Obsolete]
    class DataProvider
    {
        public IEnumerable<Models.Mod> Mods { get; private set; }

        public DataProvider()
        {
            this.Mods = ResourceManager.Mods;
        }
    }
}
