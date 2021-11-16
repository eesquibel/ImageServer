
using dotnet_etcd;
using Etcdserverpb;
using ImageServer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ImageServer
{
    using Regex = System.Text.RegularExpressions.Regex;

    public static class Config
    {
        public static string BaseDirectory { get; private set; }

        public static string ETCD_PREFIX { get; private set; }

        public static string ETCD_URL { get; private set; }

        public static readonly ConcurrentDictionary<string, Group> Groups = new ConcurrentDictionary<string, Group>();

        private static EtcdClient etcd;

        static Config()
        {
            // Read basedir from environment, or default for docker image
            BaseDirectory = Environment.GetEnvironmentVariable("BASE_DIRECTORY") ?? "/image-server";

            // ETCD base
            ETCD_PREFIX = Environment.GetEnvironmentVariable("ETCD_PREFIX") ?? "/image-server";

            // ETCD url
            ETCD_URL = Environment.GetEnvironmentVariable("ETCD_PREFIX") ?? "http://etcd:2379";

            etcd = new EtcdClient(ETCD_URL);
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

        public static async Task LoadGroup(Group group)
        {
            var result = await etcd.GetRangeAsync($"{ETCD_PREFIX}/{group.Name}/Images/");

            foreach (var pair in result.Kvs)
            {
                var key = pair.Key.ToStringUtf8();
                var value = pair.Value.ToStringUtf8();

                var parts = key.Split('/');

                if (group.ContainsKey(parts[4]))
                {
                    var image = group[parts[4]];

                    ProcessImage(image, parts.Skip(5).ToArray(), value);
                } else
                {
                    await etcd.DeleteRangeAsync($"{ETCD_PREFIX}/{group.Name}/Images/{parts[4]}");
                }
            }

            etcd.WatchRange($"{ETCD_PREFIX}/{group.Name}/Images/", (WatchEvent[] events) =>
            {
                foreach (var action in events)
                {
                    if (action.Type == Mvccpb.Event.Types.EventType.Delete)
                    {
                        continue;
                    }

                    var parts = action.Key.Split('/');

                    if (group.Name != parts[2])
                    {
                        continue;
                    }

                    if (group.ContainsKey(parts[4]))
                    {
                        var image = group[parts[4]];

                        ProcessImage(image, parts.Skip(5).ToArray(), action.Value);
                    }
                }
            });
        }

        private static void ProcessImage(Image image, string[] keys, string value)
        {
            switch (keys[0])
            {
                case "Tags":
                    var tags = JArray.Parse(value);
                    image.LoadTags(tags.Values<string>());
                    break;
                case "Reactions":
                    var reactions = JArray.Parse(value);
                    image.LoadReactions(keys[1], reactions.Values().Where(token => token is JValue).Select(token =>
                    {
                        var value = ((JValue)token).Value;

                        if (value is long || value is int)
                        {
                            value = ulong.Parse(value.ToString());
                        }

                        return value;
                    }));
                    break;
            }
        }

        public static Task UpdateImage(Image image, string key, JToken value)
        {
            return UpdateImage(image, new string[] { key }, value);
        }

        public static async Task UpdateImage(Image image, string[] keys, JToken value)
        {
            await etcd.PutAsync($"{ETCD_PREFIX}/{image.Group}/Images/{image.Name}/{string.Join('/', keys)}", value.ToString());
        }
    }
}
