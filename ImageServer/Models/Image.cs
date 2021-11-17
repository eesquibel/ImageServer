using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ImageServer.Models
{
    public class Image
    {
        /// <summary>
        /// File name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Absolute file system path to the image
        /// </summary>
        [JsonIgnore]
        public string Path { get; private set; }

        /// <summary>
        /// Absolute URI path to the image
        /// </summary>
        public string Uri => $"/raw/{Group}/{Name}";


        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; private set; }

        // TODO: What to do about video?
        /// <summary>
        /// Image format
        /// </summary>
        [JsonIgnore]
        public IImageFormat Format { get; private set; }

        /// <summary>
        /// User/API friendly name of image type
        /// </summary>
        public string Type => Format.Name.ToLower();

        /// <summary>
        /// Media width/X dimension
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Media height/Y dimension
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Reference back to the parent Group. Needed to generate the Uri
        /// </summary>
        [JsonIgnore]
        public string Group { get; private set; }

        private List<string> tags;

        public IReadOnlyList<string> Tags => tags;

        private Dictionary<string, object[]> reactions;

        public IReadOnlyDictionary<string, object[]> Reactions => reactions;

        // TODO: Helper for video object
        /// <summary>
        /// Helper function to get Image reference from the Group, FileInfo, and Image of the object
        /// </summary>
        /// <param name="group">Group parent object</param>
        /// <param name="fileInfo">FileInfo for the specific file</param>
        /// <param name="bitmap">Image object to get dimentions and format</param>
        /// <returns></returns>
        public static Image From(string group, System.IO.FileInfo fileInfo, IImageInfo imageInfo, IImageFormat imageFormat)
        {
            return new Image
            {
                Name = fileInfo.Name,
                Path = fileInfo.FullName,
                Size = fileInfo.Length,
                Format = imageFormat,
                Width = imageInfo.Width,
                Height = imageInfo.Height,
                Group = group
            };
        }

        private Image()
        {
            tags = new List<string>();
            reactions = new Dictionary<string, object[]>();
        }

        public void LoadTags(IEnumerable<string> Tags)
        {
            lock (tags)
            {
                tags.Clear();

                foreach (var tag in Tags)
                {
                    tags.Add(tag);
                }
            }
        }

        public void LoadReactions(string Key, IEnumerable<object> Reactions)
        {
            lock (reactions)
            {
                reactions[Key] = Reactions.ToArray();
            }
        }
    }
}
