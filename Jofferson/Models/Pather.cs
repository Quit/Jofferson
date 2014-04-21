using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jofferson
{
    /// <summary>
    /// Simplifies file operations
    /// </summary>
    sealed class Pather
    {
        private IList<string> parts;

        private Pather(Pather original)
        {
            this.parts = new List<string>(original.parts);
        }

        public Pather(string path)
        {
            this.parts = path.Split('/').ToList();
            if (this.parts.Count > 0 && (this.parts[0] == string.Empty))
                this.parts.RemoveAt(0);
        }

        /// <summary>
        /// Applies another
        /// </summary>
        public void Apply(Pather other)
        {
            foreach (string part in other.parts)
            {
                switch (part)
                {
                    case ".":
                        continue;
                    case "..":
                        this.Pop();
                        break;
                    default:
                        this.Add(part);
                        break;
                }
            }
        }

        public void Apply(string other)
        {
            Apply(new Pather(other));
        }

        /// <summary>
        /// Removes the last element of our queue
        /// </summary>
        public void Pop()
        {
            this.parts.RemoveAt(this.parts.Count - 1);
        }

        /// <summary>
        /// Adds a new part.
        /// </summary>
        /// <param name="part"></param>
        public void Add(string part)
        {
            this.parts.Add(part);
        }

        /// <summary>
        /// Returns the root.
        /// </summary>
        public string Root { get { return this.parts[0];  } }

        public int Count { get { return this.parts.Count; } }

        public override string ToString()
        {
            return string.Join("/", this.parts);
        }

        public string RootlessPath { get { return string.Join("/", this.parts.Skip(1)); } }
        public string Last { get { return this.parts.Last(); } }

        public static string GetWithoutRoot(string path)
        {
            int idx = path.IndexOf('/');
            if (idx == -1)
                return path;

            return path.Substring(idx + 1);
        }
    }
}
