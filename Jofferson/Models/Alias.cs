using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jofferson.Models
{
    class Alias : Reference
    {
        /// <summary>
        /// The mod we're a part of.
        /// </summary>
        public Mod Mod { get { return Origin.Mod; } }

        /// <summary>
        /// Our name (i.e. after "mod:")
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Our full name (i.e. "mod:name")
        /// </summary>
        public string FullName { get { return Mod.Name + ":" + Name; } }

        public Alias(Mod mod, string name, Resource target)
            : this(mod, name, mod.Manifest, target)
        {
        }

        public Alias(Mod mod, string name, Resource definition, Resource target)
            : base(definition, definition, target, "alias")
        {
            this.Name = name;
            mod.AddAlias(this);
        }

        public override string ToString(bool addMod, Resource resource)
        {
            return "[alias] " + FullName;
        }

        public override string ToStringSourced(bool addMod, Resource context)
        {
            return "[alias] " + FullName;
        }
    }
}
