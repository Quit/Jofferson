using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jofferson.Models;

namespace Jofferson
{

    static class ResourceManager
    {
        /// <summary>
        /// List of all mods that we are dealing with
        /// </summary>
        private static Dictionary<string, Mod> mods;
        /// <summary>
        /// Returns all mods
        /// </summary>
        public static IEnumerable<Mod> Mods { get { return mods.Values; } }
        /// <summary>
        /// List of all resources, identified by name, that we have
        /// </summary>
        private static Dictionary<string, Resource> Resources;
        
        /// <summary>
        /// List of all resources that are likely json and therefore need to be investigated
        /// </summary>
        private static Queue<Resource> Queue = new Queue<Resource>();

        /// <summary>
        /// A collection which has all files that will be created as "virtual resources" in case they do not exist.
        /// </summary>
        private static ICollection<string> VirtualResources;

        static ResourceManager()
        {

            // Try to load our virtual resources, if any
            try
            {
                using (TextReader textReader = new StreamReader("Jofferson.json"))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                {
                    VirtualResources = new HashSet<string>(JObject.ReadFrom(jsonReader).Values<string>());
                }
            }
            catch
            {
                VirtualResources = new HashSet<string>();
            }
        }

        /// <summary>
        /// Initializes the resource manager at a certain location.
        /// This location should be the mods/ directory.
        /// </summary>
        /// <param name="directory"></param>
        public static void Initialize()
        {
            mods = new Dictionary<string, Mod>();
            Resources = new Dictionary<string, Resource>();
            JsonCache = new Dictionary<Resource, JToken>();
        }

        /// <summary>
        /// Adds a new mod with a certain file system.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fs"></param>
        public static void AddMod(string name, IFileSystem fs)
        {
            if (fs == null)
                throw new ArgumentNullException("fs");

            if (mods.ContainsKey(name))
            {
                if (fs is IDisposable)
                    ((IDisposable)fs).Dispose();

                return;
            }

            mods[name] = new Mod(name, fs);
        }

        /// <summary>
        /// Cleans up, if necessary
        /// </summary>
        public static void CleanUp()
        {
            foreach (Mod mod in mods.Values)
                mod.Dispose();
        }

        /// <summary>
        /// Checks if a certain resource exists, based on <paramref name="context"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool Exists(string name, string context)
        {
            Mod mod;
            Pather p;

            if (!ConstructPather(name, context, out mod, out p))
            {
                if (ParseAlias(name) != null)
                    return true;
                return false;
            }

            return Exists(mod.FileSystem, p, false);
        }

        /// <summary>
        /// Checks if a certain resource does exist under a certain file system.
        /// </summary>
        /// <param name="system"></param>
        /// <param name="pather"></param>
        /// <param name="magicChecked"></param>
        /// <returns></returns>
        private static bool Exists(IFileSystem system, Pather pather, bool magicChecked)
        {
            // If said file exists, good
            if (system.Exists(pather.RootlessPath))
                return true;

            // If we already tried our magic
            if (magicChecked)
            {
                // Pop the magic again.
                pather.Pop();

                // Second-to-last resort: Extension is .lua, so there might be a luac somewhere.
                if (pather.Last.EndsWith(".lua") && system.Exists(pather.RootlessPath + "c"))
                    return true;

                // Last resort: .lua or .luac extension
                if (system.Exists(pather.RootlessPath + ".lua") || system.Exists(pather.RootlessPath + ".luac"))
                {
                    string script = pather.Last;
                    pather.Pop();
                    script += system.Exists(script + ".lua") ? ".lua" : ".luac";
                    pather.Add(script);
                    return true;
                }

                return false;
            }
            else
            {
                // Try it for an alias
                if (ParseAlias(pather.ToString()) != null)
                    return true;
            }

            // Since we can't check for zip files, try this.
            // It's not exactly a beautiful approach but it's an allnighter here, so \o/
            pather.Add(pather.Last + ".json");

            // Just call us again, this time without magic. If we ever redo this part, this would be nice.
            return Exists(system, pather, true);
        }

        /// <summary>
        /// Attempts to create a pather to a certain resource and returns the file system and the pather
        /// </summary>
        /// <param name="name"></param>
        /// <param name="context"></param>
        /// <param name="fs"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static bool ConstructPather(string name, string context, out Mod mod, out Pather p)
        {
            // Is it a file() path?
            if (name.StartsWith("file(") && name.EndsWith(")"))
            {   
                // Our location is relative to our current one.
                name = name.Substring(5, name.Length - 6);
                // _unless_  it starts with /. Then it's relative to the mod.
                if (name[0] == '/')
                {
                    p = new Pather(context);
                    while (p.Count > 1)
                        p.Pop();

                    // Skip the /.
                    name = name.Substring(1);
                }
                else
                    p = new Pather(context);

                p.Apply(name);
            }
            else // absolute to the mods directory... I guess?
                p = new Pather(name);

            // Get the fs
            if (!mods.TryGetValue(p.Root, out mod))
                return false;

            return true;
        }

        /// <summary>
        /// Attempts to parse <paramref name="name"/> as alias. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The alias the name represents if it exists, null otherwise</returns>
        public static Alias ParseAlias(string name)
        {
            int idx = name.IndexOf(':');
            if (idx == -1)
                return null;

            string modName = name.Substring(0, idx);
            Mod mod;

            if (!mods.TryGetValue(modName, out mod))
                return null;

            string aliasName = name.Substring(idx + 1);

            return mod.Aliases.FirstOrDefault(a => a.Name == aliasName);
        }
        /// <summary>
        /// Tries to fetch said entry. If it does not exist, creates it.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGet(string name, string context, out Resource result)
        {
            Mod mod;
            Pather p;

            // Is it an alias?
            Alias al = ParseAlias(name);
            if (al != null)
            {
                result = al.Target;
                return true;
            }

            // If we can't construct a pather then all is lost
            if (!ConstructPather(name, context, out mod, out p))
            {
                result = null;
                return false;
            }

            // The... uh... uhhhhm....
            // The... resolved path. Yes. That sounds good enough.
            var fullPath = p.ToString();

            // Try to fetch the resource from our list?
            if (Resources.TryGetValue(fullPath, out result))
                return true;

            // Checks if the path exists + adjusts our pather
            bool exists = Exists(mod.FileSystem, p, false);

            fullPath = p.ToString();

            // Try again with our new path?
            if (Resources.TryGetValue(fullPath, out result))
                return true;

            // Create a new resource then.
            string location = p.ToString();
            if (Path.GetExtension(location) == ".json")
                result = new JsonResource(location, mod, exists);
            else
                result = new Resource(location, mod, exists);
            Resources.Add(location, result);

            if (exists && result is JsonResource)
                Queue.Enqueue(result);

            return exists;
        }

        /// <summary>
        /// Characters that "identify" a string
        /// </summary>
        static readonly char[] IdentifierChars = { '/', ':' };

        public static bool IsFileReference(string text)
        {
            return text.Length >= 6 && text.Substring(0, 5) == "file(" && text.Last() == ')';
        }
        /// <summary>
        /// Checks if a string could be an identifier for a file
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsIdentifier(string text)
        {
            return IsFileReference(text) || text.IndexOfAny(IdentifierChars) >= 0 || IsInterestingFileExtension(text);
        }

        public static bool IsInterestingFileExtension(string filename)
        {
            int ind = filename.LastIndexOf('.');

            if (ind == -1)
                return false;

            switch (filename.Substring(ind))
            {
                case ".qb":
                case ".json":
                case ".png":
                case ".jpg":
                case ".html":
                case ".js":
                case ".css":
                case ".less":
                case ".lua":
                case ".cur":
                case ".luac":
                    return true;
                default:
                    return false;
            }
        }
        public static void LoadManifests()
        {
            foreach (Mod mod in mods.Values)
                mod.LoadManifest();
        }

        public static void LoadMagic()
        {
            foreach (Mod mod in mods.Values)
                mod.LoadMagic();
        }

        public static void LoadMods()
        {
            // Load the current working directory
            DirectoryInfo cwd = new DirectoryInfo(Environment.CurrentDirectory);

            // Go through all smods
            foreach (FileInfo archive in cwd.GetFiles("*.smod"))
                ResourceManager.AddMod(System.IO.Path.GetFileNameWithoutExtension(archive.Name), new ZipFS(archive.FullName));

            // Get through all directories
            foreach (DirectoryInfo directory in cwd.GetDirectories())
                try
                {
                    ResourceManager.AddMod(directory.Name, new FileFS(directory.FullName));
                }
                catch (Exception ex)
                {
                    ErrorReporter.AddError(null, null, "Cannot open mod object {0} because of {1}: {2}", directory.Name, ex.GetType().ToString(), ex.Message);

                }
        }

        /// <summary>
        /// Resolves a JSON Resource's *original* token.
        /// </summary>
        /// <param name="token"></param>
        public static JToken ResolveJson(JsonResource resource)
        {
            JToken root = resource.OriginalToken.DeepClone();
            var walker = new Providers.JsonWalker(root);
            List<JToken> keyTokens = new List<JToken>();

            walker.OnToken += t => translate(resource, t);
            //walker.OnKey += t => { var r = resolve(resource, t.Name, false); };
            walker.Work();

            return root;
        }

        /// <summary>
        /// Used in <see cref="ResolveJson"/>.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="token"></param>
        private static void translate(JsonResource resource, JToken token)
        {
            if (token.Type != JTokenType.String)
                return;

            JValue jv = (JValue)token;

            Reference reference = resolve(resource, jv.ToString(), false);

            if (reference == null)
                return;

            jv.Value = reference.Target.Location;
        }

        /// <summary>
        /// After all manifests have been loaded, we *might* want to deal with each of those files. Maybe.
        /// </summary>
        public static void ProccessQueue()
        {
            while (Queue.Count > 0)
            {
                Resource resource = Queue.Dequeue();
                
                // Try to load it.
                Pather pather = new Pather(resource.Location);
                JToken token = LoadJson(resource);

                // If the json failed
                if (token == null)
                    continue;

                var walker = new Providers.JsonWalker(token);

                walker.OnToken += t => addToContext(resource, t);
                walker.Work();
            }
        }

        private static Reference resolve(Resource context, string value, bool createIfNonExistant)
        {
            if (IsIdentifier(value))
            {
                // Get the folder we are located in (to get some context)
                Pather p = new Pather(context.Location);
                p.Pop();

                if (Exists(value, p.ToString()) || IsFileReference(value))
                {
                    // The resource definitely exists.
                    Resource res;
                    TryGet(value, p.ToString(), out res);

                    Reference reference = context.References.FirstOrDefault(r => r.Target == res);

                    // Create the reference if it didn't already exist
                    if (reference == null && createIfNonExistant)
                    {
                        // Attempt to create an alias reference
                        reference = ParseAlias(value);
                        if (reference != null)
                            context.AddReference(reference);
                        else
                            reference = new Reference(context, context, res);
                    }
                    return reference;
                }
                else
                {
                    // It IS an alias of a mod we *know*?
                    int idx = value.IndexOf(':');
                    if (idx > 0 && idx < value.Length)
                    {
                        string modName = value.Substring(0, idx);
                        Mod mod;
                        if (mods.TryGetValue(modName, out mod))
                        {
                            // The mod exists.
                            Alias alias = mod.Aliases.FirstOrDefault(a => a.FullName.Equals(value));

                            if (alias == null && createIfNonExistant && value.Length > idx + 1)
                            {
                                // Last chance: We're a virtual resource.
                                if (VirtualResources.Contains(value))
                                    alias = new Alias(mod, value.Substring(idx + 1), mod.VirtualResource);
                                else // Nope, missing.
                                    alias = new Alias(mod, value.Substring(idx + 1), mod.InvalidResource);
                            }

                            return alias;
                        }
                    }
                    // Is it a file identifier?
                    else if (IsFileReference(value))
                    {
                        // Well, I guess it's a broken one.
                        Resource res;
                        TryGet(value, p.ToString(), out res);

                        Reference reference = context.References.FirstOrDefault(r => r.Target == res);

                        // Create the reference if it didn't already exist
                        if (reference == null && createIfNonExistant)
                            reference = new Reference(context, context, res);

                        return reference;
                    }
                }
            }

            return null;
        }

        private static void addToContext(Resource resource, JToken token)
        {
            if (token.Type != JTokenType.String)
                return;


            string value = token.ToString();

            Reference reference = resolve(resource, value, true);
        }

        private static Dictionary<Resource, JToken> JsonCache;

        /// <summary>
        /// Loads a JSON resource.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static JToken LoadJson(Resource resource)
        {
            JToken result = null;

            if (JsonCache.TryGetValue(resource, out result))
                return result;

            try
            {
                using (TextReader textReader = new StreamReader(OpenRead(resource)))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                    result = JObject.ReadFrom(jsonReader);
            }
            catch (JsonReaderException ex)
            {
                ErrorReporter.AddError(resource.Mod, resource, "Parsing json {0} failed: {1} at line {2}, position {3} (path {4})", resource.Location, ex.Message, ex.LineNumber, ex.LinePosition, ex.Path);
            }
            catch (JsonException ex)
            {
                ErrorReporter.AddError(resource.Mod, resource, "Parsing json {0} failed: {1}", resource.Location, ex.Message);
            }
            catch (IOException ex)
            {
                ErrorReporter.AddError(resource.Mod, resource, "Opening {0} to parse json failed: {1}", resource.Location, ex.Message);
            }

            JsonCache.Add(resource, result);

            return result;
        }

        public static Stream OpenRead(Resource resource)
        {
            return resource.Mod.FileSystem.OpenRead(new Pather(resource.Location).RootlessPath);
        }
    }
}
