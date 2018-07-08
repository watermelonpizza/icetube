using IceTube.DataModels;
using IceTube.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IceTube.Tasks
{
    public class YoutubeChannelFeedTask : ITask, IHostedService, IDisposable
    {
        public const string YoutubeChannelFeedTaskName = "YoutubeChannelFeed";
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        private readonly ILogger<YoutubeChannelFeedTask> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private Timer _timer;
        private bool _isRunning = false;

        public YoutubeChannelFeedTask(ILogger<YoutubeChannelFeedTask> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public bool QueueNow(CancellationToken cancellationToken, out string error)
        {
            error = string.Empty;

            if (!_isRunning)
            {
                var result = false;

                try
                {
                    result = _timer.Change(TimeSpan.Zero, Interval);
                    _logger.LogInformation("Youtube channel feed task was requested to run now, changed timer to run immediatly");

                    return result;
                }
                catch (Exception e)
                {
                    error = "Failed to update Youtube channel feed task when requested.";
                    _logger.LogError(e, error);

                    error += $" error: {e.Message}";

                    return result;
                }
            }
            else
            {
                error = "Youtube channel feed task was requested to run now, but the task is currently running. Skipping.";
                _logger.LogInformation(error);

                return false;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Youtube channel feed task task is starting.");

            using (var scope = _scopeFactory.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                var task = await context.Tasks.FindAsync(new object[] { YoutubeChannelFeedTaskName }, cancellationToken);

                if (task == null)
                {
                    _logger.LogDebug("Youtube channel feed task was not found in the database. Creating an entry now");

                    task = new IceTubeTask
                    {
                        TaskName = YoutubeChannelFeedTaskName
                    };

                    await context.Tasks.AddAsync(task, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                TimeSpan dueTime = TimeSpan.Zero;

                if (task.LastRan.HasValue)
                {
                    dueTime = Interval - (DateTime.UtcNow - task.LastRan.Value);

                    if (dueTime.Ticks < 0)
                    {
                        _logger.LogDebug("Youtube channel feed task was last ran older than the interval time. Set to run in 1 minute");
                        dueTime = TimeSpan.FromMinutes(1));
                    }

                    _logger.LogInformation("Youtube channel feed task has a previous ran time, calculating next run time to be in {dueTime}", dueTime);
                }

                _logger.LogDebug("Setting timer");
                _timer = new Timer(UpdateFeeds, null, dueTime, Interval);
            }
        }

        private async void UpdateFeeds(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            using (var googleAccount = scope.ServiceProvider.GetRequiredService<GoogleAccount>())
            {
                if (!await googleAccount.HasSetup())
                {
                    _logger.LogWarning("Youtube channel feed task wanted to run but user hasn't setup their account. Skipping.");

                    return;
                }

                _isRunning = true;

                using (var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    var task = await context.Tasks.FindAsync(YoutubeChannelFeedTaskName);

                    if (task == null)
                    {
                        _logger.LogError("Attempted to update task status but couldn't find task. This will cause instability issues in the future. Best fix would probably be to restart the application.");
                    }

                    try
                    {
                        _logger.LogInformation("Updating Youtube channel feeds");

                        var youtubeChannels = await context.Channels.ToArrayAsync();

                        foreach (var channel in youtubeChannels)
                        {
                            _logger.LogInformation("Getting feed for channel '{channelName}'", channel.Name);

                            var youtubeActivities = await googleAccount.GetChannelActivityAsync(channel);

                            // Update the channels last checked date as the channel feed update can take many minutes,
                            // or in the case the update was interrupted, we don't want to collect unessasary activity feeds
                            // for channels we already have updated to a specific date.
                            channel.LastCheckedAt = DateTime.UtcNow;

                            // Get all the upload acivities which we don't already have
                            foreach (var activity in 
                                youtubeActivities.Where(x => x.Type == YoutubeActivityType.Upload && !channel.Videos.Any(v => v.ActivityId == x.Id)))
                            {
                                _logger.LogInformation("New activity detected, adding upload {@newactivity}", activity);

                                await context.Videos.AddAsync(
                                    new YoutubeVideo
                                    {
                                        ActivityId = activity.Id,
                                        PublishedAt = activity.PublishedAt,
                                        AddedAt = DateTime.UtcNow,
                                        Title = activity.Title,
                                        Description = activity.Description,
                                        ThumbnailUrl = activity.ThumbnailUrl,
                                        VideoId = activity.VideoId,
                                        DownloadState = VideoDownloadState.NotStarted,
                                        ChannelId = channel.Id
                                    });
                            }
                        }

                        _logger.LogDebug("Saving updated Youtube channel data");
                        await context.SaveChangesAsync();
                        _logger.LogInformation("Saved updated Youtube channel data");

                        _logger.LogDebug("Updating task ran status");

                        if (task != null)
                        {
                            task.LastRan = DateTime.UtcNow;
                            task.LastRanSuccess = true;
                            task.LastRanStatus = "Task completed successfully";

                            await context.SaveChangesAsync();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Youtube channel feed task unexpectedly threw an exception.");

                        if (task != null)
                        {
                            task.LastRan = DateTime.UtcNow;
                            task.LastRanSuccess = false;
                            task.LastRanStatus = JsonConvert.SerializeObject(e);

                            await context.SaveChangesAsync();
                        }
                    }
                    finally
                    {
                        _isRunning = false;
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Youtube channel feed task is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
