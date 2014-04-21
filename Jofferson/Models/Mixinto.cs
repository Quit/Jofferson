using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jofferson.Models
{
    class Mixinto : Reference
    {
        public Mixinto(Resource definition, Resource origin, Resource target, string prefix)
            : base(definition, origin, target, "mixinto")
        {
        }
    }
}
