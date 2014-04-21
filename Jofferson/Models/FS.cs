using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Jofferson
{
    /// <summary>
    /// Implements very simple file system operations
    /// </summary>
    interface IFileSystem
    {
        /// <summary>
        /// Checks if a file in this environment exists.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>true if the file exists, false otherwise</returns>
        bool Exists(string filename);
        
        /// <summary>
        /// Opens a file in this environment.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        Stream OpenRead(string filename);

        /// <summary>
        /// Returns the full name of this resource in a way that it may be identified.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        string FullName(string filename);

        /// <summary>
        /// Can this FS be accessed with the explorer?
        /// </summary>
        bool IsAccessible { get; }
    }

    /// <summary>
    /// Implements a FS that operates on a zip (i.e. smod)
    /// </summary>
    class ZipFS : IFileSystem, IDisposable
    {
        private string archiveName;
        private string rootFolder;
        private ZipArchive zipArchive;

        public ZipFS(string filename)
        {
            this.archiveName = filename;
            this.rootFolder = Path.GetFileNameWithoutExtension(filename);
            this.zipArchive = ZipFile.OpenRead(filename);
        }

        public bool Exists(string filename)
        {
            return GetEntry(filename) != null;
        }

        public Stream OpenRead(string filename)
        {
            return GetEntry(filename).Open();
        }

        public string FullName(string filename)
        {
            return Path.Combine(this.archiveName, filename);
        }

        public bool IsAccessible { get { return false; } }

        /// <summary>
        /// Returns an entry from the archive.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private ZipArchiveEntry GetEntry(string filename)
        {
            // TODO: Find out if .NET is actually constant (in the sense that it is *always* DirectorySeperatorChar)
            filename = string.Concat(this.rootFolder, Path.DirectorySeparatorChar, filename);
            return zipArchive.GetEntry(filename.Replace("/", @"\")) ?? zipArchive.GetEntry(filename.Replace(@"\", "/"));
        }

        public void Dispose()
        {
            this.zipArchive.Dispose();
        }
    }

    /// <summary>
    /// Implements the FS that represents a mod directory
    /// </summary>
    class FileFS : IFileSystem
    {
        /// <summary>
        /// Directory that all this stuff happens in.
        /// </summary>
        public string Directory { get; private set; }

        public FileFS(string directory)
        {
            this.Directory = directory;
        }

        public bool Exists(string filename)
        {
            return File.Exists(FullName(filename));
        }

        public Stream OpenRead(string filename)
        {
            return File.OpenRead(FullName(filename));
        }

        public bool IsAccessible { get { return true; } }

        /// <summary>
        /// Returns an absolute path based on a relative path.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string FullName(string filename)
        {
            return Path.Combine(this.Directory, filename);
        }
    }
}
