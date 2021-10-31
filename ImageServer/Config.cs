
using ImageServer.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ImageServer
{
    using Regex = System.Text.RegularExpressions.Regex;

    public static class Config
    {
        public static string BaseDirectory { get; private set; }

        public static readonly ConcurrentDictionary<string, Group> Groups = new ConcurrentDictionary<string, Group>();

        static Config()
        {
            // Read basedir from environment, or default for docker image
            BaseDirectory = Environment.GetEnvironmentVariable("BASE_DIRECTORY") ?? "/image-server";
        }

        public static bool TryGetGroup(string key, out Group value) => Groups.TryGetValue(key, out value);

        public static bool MapToGroup(FileInfo file, out Group group) => Groups.TryGetValue(file.Directory.Name, out group) && group.Path == file.Directory.FullName;

        static readonly Regex SHA1Test = new Regex("^[a-fA-F0-9]{40}$");

        static readonly SHA1 SHA1HashAlgorithm = SHA1.Create();

        public static bool IsSHA1(string test) => SHA1Test.IsMatch(test);

        public static bool IsSHA1(FileInfo test) => SHA1Test.IsMatch(Path.GetFileNameWithoutExtension(test.Name));

        public static async Task<string> HashFile(FileInfo file)
        {
            using (var reader = file.OpenRead())
            {
                var hash = await SHA1HashAlgorithm.ComputeHashAsync(reader);
                return BitConverter.ToString(hash).ToLower().Replace("-", "");
            }
        }
    }
}
