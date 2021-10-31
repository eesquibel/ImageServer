using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageServer.Models
{
    public class Group
    {
        public string Name { get; private set; }
        public string Path { get; private set; }

        [JsonIgnore]
        public ConcurrentDictionary<string, Image> Images { get; private set; }

        /// <summary>
        /// Helper function to get Group instance from a DirectoryInfo object
        /// </summary>
        /// <param name="directoryInfo">Directory containing images</param>
        /// <returns>A new Group model for the provided directory</returns>
        public static Group From(DirectoryInfo directoryInfo)
        {
            return new Group
            {
                Name = directoryInfo.Name,
                Path = directoryInfo.FullName
            };
        }

        private Group()
        {
            Images = new ConcurrentDictionary<string, Image>();
        }

        private bool TryAdd(string key, Image value) => Images.TryAdd(key, value);

        public bool TryAdd(FileInfo fileInfo, System.Drawing.Image bitmap, out Image image)
        {
            image = Image.From(Name, fileInfo, bitmap);
            return TryAdd(image.Name, image);
        }

        public bool TryRemove(string key, out Image value) => Images.TryRemove(key, out value);

        public bool TryRemove(FileInfo fileInfo, out Image value) => Images.TryRemove(fileInfo.Name, out value);

        public bool TryGetImage(string key, out Image value) => Images.TryGetValue(key, out value);

        public bool ContainsKey(string key) => Images.ContainsKey(key);

        public bool ContainsKey(FileInfo fileInfo) => Images.ContainsKey(fileInfo.Name);

        /// <summary>
        /// Load the contents of the directory
        /// </summary>
        public void Load()
        {
            // Iterate through all the files in the directory
            foreach (var path in Directory.GetFiles(Path))
            {
                try
                {
                    // Get FileInfo and Image object for file
                    // TODO: Handle video formats
                    // TODO: Handle invalid file types
                    var (file, bitmap) = Tuple.Create(new FileInfo(path), System.Drawing.Image.FromFile(path));

                    // Add to thread-safe collection
                    TryAdd(file, bitmap, out _);

                    // Cleanup
                    bitmap.Dispose();
                }
                catch (Exception)
                {
                    // TODO: Handle Load exceptions
                }
            }
        }
    }
}
