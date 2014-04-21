using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jofferson.Models
{
    class Reference
    {
        /// <summary>
        /// Where this reference was defined
        /// </summary>
        public Resource Definition { get; private set; }
        
        /// <summary>
        /// The origin of this reference. This can be the same as Definition.
        /// </summary>
        public Resource Origin { get; private set; }

        /// <summary>
        /// The target of the reference.
        /// </summary>
        public Resource Target { get; private set; }

        public Reference(Resource definition, Resource origin, Resource target, string prefix)
        {
            this.Definition = definition;
            this.Origin = origin;
            this.Target = target;
            this.Prefix = prefix;

            this.Definition.AddReference(this);

            if (this.Definition != this.Origin)
                this.Origin.AddReference(this);

            this.Target.AddReferredBy(this);
        }

        public Reference(Resource definition, Resource origin, Resource target): this(definition, origin, target, string.Empty)
        {
        }

        /// <summary>Prefix that is attached to our DisplayName.</summary>
        public string Prefix { get; protected set; }
        public bool HasPrefix { get { return !String.IsNullOrEmpty(Prefix); } }

        public virtual bool Valid { get { return Origin.Exists && Target.Exists; } }

        public virtual string ToString(bool addMod, Resource context)
        {
            StringBuilder sb = new StringBuilder();

            if (addMod || HasPrefix)
            {
                sb.Append('[');

                if (addMod)
                {
                    sb.Append(this.Definition.Mod.Name);
                    if (HasPrefix)
                        sb.Append(' ');
                }

                if (HasPrefix)
                    sb.Append(Prefix);

                sb.Append("] ");
            }

            // If we are a "pointer", such as an override or mixinto
            if (this.Origin != this.Definition && this.Origin != context)
                sb.Append(this.Origin.Location).Append("\n\t->\n\t");
            
            sb.Append(this.Target.Location);
            
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string that shows where this reference is coming from.
        /// </summary>
        /// <param name="addMod"></param>
        /// <returns></returns>
        public virtual string ToStringSourced(bool addMod, Resource context)
        {
            StringBuilder sb = new StringBuilder();

            if (addMod || HasPrefix)
            {
                sb.Append('[');

                if (addMod)
                {
                    sb.Append(this.Definition.Mod.Name);
                    if (HasPrefix)
                        sb.Append(' ');
                }

                if (HasPrefix)
                    sb.Append(Prefix);

                sb.Append("] ");
            }

            if (context == this.Origin)
                sb.Append(this.Target.Location);
            else
                sb.Append(this.Origin.Location);

            if (this.Definition != this.Origin)
                sb.Append("\n\t(@ ").Append(this.Definition.Location).Append(")");

            return sb.ToString();            
        }

        public string ToStringSourced(bool addMod)
        {
            return this.ToStringSourced(addMod, null);
        }

        public override string ToString()
        {
            return ToString(false, null);
        }
    }
}
