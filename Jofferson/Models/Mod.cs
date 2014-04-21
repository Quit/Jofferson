using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Jofferson.Models
{
    sealed class Mod : IDisposable
    {
        /// <summary>
        /// Name of this mod. This is either the folder name *or* the archive name (minus .smod).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The full path to this mod's folder *or* smod file.
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Returns the path to the manifest.
        /// </summary>
        public string ManifestFileName { get { return FileSystem.FullName("manifest.json"); } }

        /// <summary>
        /// Manifest resource
        /// </summary>
        public Manifest Manifest { get; private set; }

        /// <summary>
        /// The file system that we're working on
        /// </summary>
        public IFileSystem FileSystem { get; private set; }

        /// <summary>
        /// List of all resources
        /// </summary>
        public ICollection<Resource> Resources { get; private set; }

        /// <summary>
        /// List of all aliases
        /// </summary>
        public ICollection<Alias> Aliases { get; private set; }

        /// <summary>
        /// List of all overrides
        /// </summary>
        public ICollection<Reference> Overrides { get; private set; }

        /// <summary>
        /// List of all mixintos
        /// </summary>
        public ICollection<Reference> Mixintos { get; private set; }

        /// <summary>
        /// List of all errors
        /// </summary>
        public ICollection<Error> Errors { get; private set; }

        /// <summary>
        /// Whether this mod is still valid or not
        /// </summary>
        public bool Valid { get; private set; }

        /// <summary>
        /// If the mod is empty
        /// </summary>
        public bool Empty { get { return Aliases.Count == 0 && Overrides.Count == 0 && Mixintos.Count == 0 && Resources.Count == 0; } }

        /// <summary>
        /// Our manifest jtoken
        /// </summary>
        private JToken manifestRoot;

        /// <summary>
        /// Backing field: What we use to describe an invalid resource
        /// </summary>
        private Resource invalidResource;

        public Resource InvalidResource
        {
            get
            {
                if (invalidResource == null)
                    invalidResource = new Resource("unknown", this, false);
                return invalidResource;
            }
        }

        public Mod(string name, IFileSystem system)
        {
            this.Name = name;
            this.FileSystem = system;
            this.Resources = new List<Resource>();
            this.Aliases = new List<Alias>();
            this.Overrides = new List<Reference>();
            this.Mixintos = new List<Reference>();
            this.Errors = new List<Error>();

            this.Manifest = new Manifest(this);
            this.Valid = this.Manifest.Exists;
        }

        public void CheckValidity()
        {
            // We can only get worse.
            if (!Valid)
                return;

            Valid = this.Errors.Count == 0 && Resources.All(r => r.Valid) && Aliases.All(a => a.Valid) && Overrides.All(a => a.Valid);
        }

        public void Dispose()
        {
            if (FileSystem is IDisposable)
                ((IDisposable)FileSystem).Dispose();
        }

        public void LoadManifest()
        {
            if (!this.Manifest.Exists)
                return;

            // Try to open it for Json?
            manifestRoot = ResourceManager.LoadJson(this.Manifest);


            // Try to read "aliases".
            JToken token = manifestRoot["aliases"];

            if (token != null && token.Type == JTokenType.Object)
                parseAliases(token);
        }

        private bool validateType(JToken token, JTokenType type, Resource resource, string format)
        {
            if (token == null)
                return false;

            if (token.Type == type)
                return true;
            else
                ErrorReporter.AddError(this, resource, format, token.Type);

            return false;
        }

        private readonly string[] uiElements = { "html", "js", "less" };

        public void LoadMagic()
        {
            if (this.manifestRoot == null)
                return;

            JToken token = manifestRoot["overrides"];

            if (validateType(token, JTokenType.Object, this.Manifest, "overrides has invalid type {0}"))
                parseReferences((JObject)token, (key, res) => this.Overrides.Add(new Reference(this.Manifest, res, key, "override")));

            token = manifestRoot["mixintos"];
            if (validateType(token, JTokenType.Object, this.Manifest, "mixintos has invalid type {0}"))
                parseReferences((JObject)token, (key, res) => this.Mixintos.Add(new Models.Mixinto(this.Manifest, res, key, "mixinto")));

            token = manifestRoot["ui"];

            if (validateType(token, JTokenType.Object, this.Manifest, "ui has invalid type {0}"))
            {
                foreach (string key in uiElements)
                {
                    JToken subToken = token[key];

                    if (validateType(subToken, JTokenType.Array, this.Manifest, string.Concat("ui.", key, " has invalid type {0}")))
                        parseReferences((JArray)subToken, (res) => new Reference(this.Manifest, this.Manifest, res, key));
                }
            }

            token = manifestRoot["functions"];

            if (validateType(token, JTokenType.Object, this.Manifest, "functions has invalid type {0}"))
            {
                foreach (var func in (JObject)token)
                {
                    if (validateType(func.Value, JTokenType.Object, this.Manifest, string.Concat("Function ", func.Key, " has invalid type {0}")))
                    {
                        // Technically it's not an alias, but... we'll let it slip for now
                        JObject funcDef = (JObject)func.Value;
                        if (validateType(funcDef["controller"], JTokenType.String, this.Manifest, string.Concat("Function controller ", func.Key, " has invalid type {0}")))
                        {
                            Resource res;
                            if (!ResourceManager.TryGet(funcDef["controller"].ToString(), this.Name, out res))
                            {
                                ErrorReporter.AddError(this, this.Manifest, "Unable to find controller for {0}", func.Key);
                            }
                            else
                                new Alias(this, func.Key, res);
                        }
                    }
                }
            }

            token = manifestRoot["components"];
            if (validateType(token, JTokenType.Object, this.Manifest, "components has invalid type {0}"))
            {
                foreach (var component in (JObject)token)
                {
                    if (validateType(component.Value, JTokenType.String, this.Manifest, string.Concat("Component ", component.Key, " has invalid type {0}")))
                    {
                        Resource res;
                        if (!ResourceManager.TryGet(component.Value.ToString(), this.Name, out res))
                        {
                            ErrorReporter.AddError(this, this.Manifest, "Unable to find component for {0}", component.Key);
                        }
                        else
                            new Alias(this, component.Key, res);
                    }
                }
            }

            // server_init_script // client_init_script
            foreach (string script in new string[] { "server_init_script", "client_init_script" })
            {
                token = manifestRoot[script];

                if (validateType(token, JTokenType.String, this.Manifest, script + "has invalid type {0}"))
                {
                    Resource res;
                    if (!ResourceManager.TryGet(token.ToString(), this.Name, out res))
                        ErrorReporter.AddError(this, this.Manifest, "Unable to find file for {0} ({1})", script, token.ToString());
                    else
                        new Reference(this.Manifest, this.Manifest, res);
                }
            }
            manifestRoot = null;

            CheckValidity();
        }

        /// <summary>Parses the aliases section</summary>
        /// <param name="aliases"></param>
        private void parseAliases(JToken aliases)
        {
            foreach (JProperty property in aliases)
            {
                if (property.Value.Type != JTokenType.String)
                {
                    ErrorReporter.AddError(this, this.Manifest, "Invalid alias value for {0}:{1}: expected string, got {2}", Name, property.Name, property.Value.Type);
                    continue;
                }

                // Try to load the resource.
                Resource res;
                if (!ResourceManager.TryGet(property.Value.ToString(), this.Name, out res))
                {
                    // Invalid formatted?
                    if (res == null)
                    {
                        ErrorReporter.AddError(this, this.Manifest, "Invalid alias value for {0}:{1}: unknown string {2}", Name, property.Name, property.Value.ToString());
                        continue;
                    }

                    // The resource was simply missing.
                    ErrorReporter.AddError(this, this.Manifest, "Missing alias resource for {0}:{1}: {2} not found", Name, property.Name, res.Location);
                }

                new Alias(this, property.Name, res);
            }
        }

        public void AddAlias(Alias alias)
        {
            Aliases.Add(alias);
        }

        private void addReference(Resource keyRes, string value, Action<Resource, Resource> action)
        {
            Resource valueRes;

            if (!ResourceManager.TryGet(value, this.Name, out valueRes))
            {
                if (valueRes == null)
                {
                    ErrorReporter.AddError(this, this.Manifest, "Invalid reference value for {0}: unknown... stuff.", value);
                    return;
                }
                else
                    ErrorReporter.AddError(this, this.Manifest, "Missing resource for {0}", value);
            }

            action(keyRes, valueRes);
        }

        private void parseReferences(JArray container, Action<Resource> action)
        {
            foreach (var element in container)
            {
                Resource res;

                if (validateType(element, JTokenType.String, this.Manifest, "Invalid reference element of type {0}"))
                {
                    if (!ResourceManager.TryGet(element.ToString(), this.Name, out res))
                    {
                        if (res == null)
                        {
                            ErrorReporter.AddError(this, this.Manifest, "Invalid reference element {0}", element);
                        }
                        else
                            ErrorReporter.AddError(this, this.Manifest, "Invalid reference {0}: Not found.", element);

                        // TODO: Add it anyway?
                        continue;
                    }

                    action(res);
                }
            }
        }

        private void parseReferences(JObject container, Action<Resource, Resource> action)
        {
            foreach (var property in container)
            {
                Resource keyRes;

                if (!ResourceManager.TryGet(property.Key, this.Name, out keyRes))
                {
                    if (keyRes == null)
                    {
                        ErrorReporter.AddError(this, this.Manifest, "Invalid reference key {0}.", property.Key);
                        continue; // TODO: Add it anyway?
                    }

                    ErrorReporter.AddError(this, this.Manifest, "Invalid reference key {0}: Not found", property.Value);
                }

                if (property.Value.Type == JTokenType.String)
                    addReference(keyRes, property.Value.ToString(), action);
                else if (property.Value.Type == JTokenType.Array)
                {
                    foreach (var token in property.Value)
                    {
                        if (token.Type == JTokenType.String)
                            addReference(keyRes, token.ToString(), action);
                        else
                            ErrorReporter.AddError(this, this.Manifest, "Invalid reference value type {0} in array of {1}", token.Type, property.Key);
                    }
                }
                else
                    ErrorReporter.AddError(this, this.Manifest, "Invalid reference value type {0} for {1}", property.Value.Type, property.Key);
            }
        }

        public void AddResource(Resource res)
        {
            if (res.Mod != this)
                throw new ArgumentException("resource must belong to this mod", "res");

            this.Resources.Add(res);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
