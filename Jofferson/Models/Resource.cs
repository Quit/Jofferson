using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Jofferson.Models
{
    class Resource
    {
        // TODO: make this prettier as a file or something like that
        // Fake resources will be displayed as existing, but not valid.
        // This won't invalidate things pointing to them, but they themselves will be displayed as broken.
        private static ICollection<string> FakeResources = new HashSet<string>
        {
            "stonehearth:customization_variants"
        };

        /// <summary>
        /// Outgoing references
        /// </summary>
        public ICollection<Reference> References { get; private set; }
        /// <summary>
        /// Incoming references
        /// </summary>
        public ICollection<Reference> ReferredBy { get; private set; }

        /// <summary>
        /// Mod this resource is located at
        /// </summary>
        public Mod Mod { get; private set; }
        
        /// <summary>
        /// If this resource exists (i.e. is not merely a "fake" resource)
        /// </summary>
        public bool Exists { get; private set; }

        /// <summary>
        /// The location of this resource; absolute path.
        /// </summary>
        public string Location { get; private set; }

        /// <summary>
        /// Returns the full path to this resource
        /// </summary>
        public string FullLocation { get { return Mod.FileSystem.FullName(Pather.GetWithoutRoot(Location)); } }
        
        /// <summary>
        /// Returns whether this resource contained not-so-nice references.
        /// </summary>
        public bool Valid { get; private set; }

        public Resource(string location, Mod mod, bool exists)
        {
            this.Location = location;
            this.Mod = mod;
            
            if (!exists && FakeResources.Contains(location))
            {
                this.Exists = true;
                this.Valid = false;
            }
            else
            {
                this.Exists = exists;
                this.Valid = Exists;
            }

            this.References = new List<Reference>();
            this.ReferredBy = new List<Reference>();

            mod.AddResource(this);
        }

        /// <summary>
        /// Creates a new, existing resource
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mod"></param>
        public Resource(string location, Mod mod)
            : this(location, mod, true)
        { }

        /// <summary>
        /// Adds a new reference (i.e. this resource points to something else)
        /// </summary>
        /// <param name="reference"></param>
        public void AddReference(Reference reference)
        {
            this.References.Add(reference);

            if (!reference.Valid)
                Invalidate();
        }

        /// <summary>
        /// Adds a new referred by value (i.e. this resource was referred by something else)
        /// </summary>
        /// <param name="referee"></param>
        public virtual void AddReferredBy(Reference referee)
        {
            this.ReferredBy.Add(referee);
        }

        public override string ToString()
        {
            return this.Location;
        }

        public void Invalidate()
        {
            if (!this.Valid)
                return;

            this.Valid = false;
            this.Mod.CheckValidity();
        }
    }

    class Manifest : JsonResource
    {
        public Manifest(Mod mod)
            : base(mod.Name + "/manifest.json", mod, mod.FileSystem.Exists("manifest.json"))
        {
            if (!Exists)
                ErrorReporter.AddError(mod, this, "Manifest missing.");
        }
    }
}
