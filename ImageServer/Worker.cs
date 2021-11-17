using ImageServer.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageServer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> LOG;

        public Worker(ILogger<Worker> logger)
        {
            LOG = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LOG.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            // Watch all file system changes in basedir
            using (var watch = new FileSystemWatcher(Config.BaseDirectory)
            {
                // Need to watch all subdirs to catch change changes in all groups
                IncludeSubdirectories = true,
                // Fire events to update various Group's Image collections
                EnableRaisingEvents = true
            })
            {
                // Watch each action
                watch.Created += Watch_Created;
                watch.Deleted += Watch_Deleted;
                watch.Renamed += Watch_Renamed;

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }

                watch.Created -= Watch_Created;
                watch.Deleted -= Watch_Deleted;
                watch.Renamed -= Watch_Renamed;
            }
        }

        private async void Watch_Renamed(object sender, RenamedEventArgs renamed)
        {
            try
            {
                var old = new FileInfo(renamed.OldFullPath);
                var current = new FileInfo(renamed.FullPath);

                // Ignore directory changes
                if (current.Attributes.HasFlag(FileAttributes.Directory))
                {
                    return;
                }

                // See if file is an a direct subdir of basedir and return its Group
                if (Config.MapToGroup(current, out Group group))
                {
                    // Make sure its a file we have cataloged
                    if (group.ContainsKey(old))
                    {
                        // TODO: Handle video files
                        var (imageInfo, format) = await SixLabors.ImageSharp.Image.IdentifyWithFormatAsync(renamed.FullPath);

                        // Stop other threads (i.e. REST API) from accessing the Group until we are done since
                        // this takes two opperations
                        lock (group)
                        {
                            // Get the old file out of the Group
                            if (group.TryRemove(old, out Image image))
                            {
                                // Re-add the file at the new path
                                if (group.TryAdd(current, imageInfo, format, out image))
                                {
                                    LOG.LogInformation("Renamed: [{0}] {1} => {2}", group.Name, old.Name, image.Name);
                                }
                                else
                                {
                                    LOG.LogError("Renamed add Failed: [{0}] {1}", group.Name, current.Name);
                                }
                            }
                            else
                            {
                                LOG.LogError("Renamed removal Failed: [{0}] {1}", group.Name, current.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOG.LogError(e, e.Message);
            }
        }

        private void Watch_Deleted(object sender, FileSystemEventArgs deleted)
        {
            try
            {
                var file = new FileInfo(deleted.FullPath);

                // Ignore directory changes
                if (file.Attributes.HasFlag(FileAttributes.Directory))
                {
                    return;
                }

                // See if file is an a direct subdir of basedir and return its Group
                if (Config.MapToGroup(file, out Group group))
                {
                    // Make sure its a file we have cataloged 
                    if (group.ContainsKey(file))
                    {
                        // Get the old file out of the Group
                        if (group.TryRemove(file, out Image image))
                        {
                            LOG.LogInformation("Removed: [{0}] {1}", group.Name, image.Name);
                        } else
                        {
                            LOG.LogError("Removed Failed: [{0}] {1}", group.Name, file.Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOG.LogError(e, e.Message);
            }
        }

        private async void Watch_Created(object sender, FileSystemEventArgs created)
        {
            try
            {
                var file = new FileInfo(created.FullPath);

                // Ignore directory changes
                if (file.Attributes.HasFlag(FileAttributes.Directory))
                {
                    return;
                }

                // See if file is an a direct subdir of basedir and return its Group
                if (Config.MapToGroup(file, out Group group))
                {
                    // Check if the filename is a already in SHA1 + extension format
                    if (!Config.IsSHA1(file))
                    {
                        try
                        {
                            // Hash file, and rename to SHA1 + extension
                            var hash = await Config.HashFile(file);
                            var name = hash + file.Extension;
                            var destination = Path.Combine(file.DirectoryName, name);
                            file.MoveTo(destination, false);

                        } catch(Exception e)
                        {
                            LOG.LogError(e, e.Message);
                        }
                    }

                    // TODO: Handle video files
                    var (imageInfo, format) = await SixLabors.ImageSharp.Image.IdentifyWithFormatAsync(file.FullName);

                    // Add the file to the group
                    if (group.TryAdd(file, imageInfo, format, out Image image))
                    {
                        LOG.LogInformation("Added: [{0}] {1}", group.Name, image.Name);
                    }
                    else
                    {
                        LOG.LogError("Added Failed: [{0}] {1}", group.Name, file.Name);
                    }
                }
            } catch (Exception e)
            {
                LOG.LogError(e, e.Message);
            }
        }
    }
}
