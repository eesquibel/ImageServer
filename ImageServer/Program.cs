using ImageServer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Make sure base directory exists...
            if (Directory.Exists(Config.BaseDirectory))
            {
                // Iterate through all of its sub directories, returning a DirectoryInfo object
                foreach (var source in Directory.GetDirectories(Config.BaseDirectory).Select(path => new DirectoryInfo(path)))
                {
                    // Get group model from DirectoryInfo
                    var group = Group.From(source);

                    if (Config.Groups.TryAdd(source.Name, group))
                    {
                        // Load the directory asynchronously 
                        Task.Run(group.Load);
                    }
                }
            }

            // Start the worker and server
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    // Log to console for docker
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Start the file watch worker
                    services.AddHostedService<Worker>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Start the API service
                    webBuilder.UseStartup<Startup>();
                });
    }
}
