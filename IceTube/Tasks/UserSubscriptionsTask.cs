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
    public class UserSubscriptionsTask : ITask, IHostedService, IDisposable
    {
        public const string UserSubscriptionsTaskName = "UserSubscriptions";
        public static readonly TimeSpan Interval = TimeSpan.FromDays(7);

        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private Timer _timer;
        private bool _isRunning = false;

        public UserSubscriptionsTask(ILogger<UserSubscriptionsTask> logger, IServiceScopeFactory scopeFactory)
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
                    _logger.LogInformation("User subscriptions update task was requested to run now, changed timer to run immediatly");

                    return result;
                }
                catch (Exception e)
                {
                    error = "Failed to update user subscriptions update task when requested.";
                    _logger.LogError(e, error);

                    error += $" error: {e.Message}";

                    return result;
                }
            }
            else
            {
                error = "User subscriptions update task was requested to run now, but the task was already running. Skipping.";
                _logger.LogInformation(error);

                return false;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("User subscriptions update task is starting.");

            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var task = await context.Tasks.FindAsync(new object[] { UserSubscriptionsTaskName }, cancellationToken);

                if (task == null)
                {
                    _logger.LogDebug("Update task was not found in the database. Creating an entry now");

                    task = new IceTubeTask
                    {
                        TaskName = UserSubscriptionsTaskName
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
                        _logger.LogDebug("Update task was last ran older than the interval time. Set to run in 5 minutes");
                        dueTime = TimeSpan.FromMinutes(5);
                    }

                    _logger.LogInformation("Update task has a previous ran time, calculating next run time to be in {dueTime}", dueTime);
                }

                _timer = new Timer(UpdateFeeds, null, dueTime, Interval);
            }
        }

        private async void UpdateFeeds(object state)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var googleAccount = scope.ServiceProvider.GetRequiredService<GoogleAccount>();

                if (!await googleAccount.HasSetup())
                {
                    _logger.LogWarning("User subscriptions update task wanted to run but user hasn't setup their account. Skipping.");

                    return;
                }

                _isRunning = true;

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var task = await context.Tasks.FindAsync(UserSubscriptionsTaskName);

                if (task == null)
                {
                    _logger.LogError("Attempted to update task status but couldn't find task. This will cause instability issues in the future. Best fix would probably be to restart the application.");
                }

                try
                {
                    _logger.LogInformation("Updating users subscription feed");

                    var youtubeSubscriptions = await googleAccount.GetSubscriptionsAsync();
                    var iceTubeSubscriptions = await context.Subscriptions.ToArrayAsync();

                    foreach (var youtubeSubscription in youtubeSubscriptions)
                    {
                        var existingSubscription = iceTubeSubscriptions.FirstOrDefault(x => x.Id == youtubeSubscription.Id);

                        if (existingSubscription == null)
                        {
                            _logger.LogInformation("New subscription detected, adding subscription: {@newsubscription}", youtubeSubscription);

                            await context.Subscriptions.AddAsync(
                                new Subscription
                                {
                                    Id = youtubeSubscription.Id,
                                    Name = youtubeSubscription.Name,
                                    Description = youtubeSubscription.Description
                                });
                        }
                        else if (youtubeSubscription.Name != existingSubscription.Name || youtubeSubscription.Description != existingSubscription.Description)
                        {
                            _logger.LogInformation("Updating existing subscription metadata: {@oldsubscription} => to => {@newsubscription}", existingSubscription, youtubeSubscription);

                            existingSubscription.Name = youtubeSubscription.Name;
                            existingSubscription.Description = youtubeSubscription.Description;
                        }
                    }

                    foreach (var oldSubscription in iceTubeSubscriptions.Where(x => !youtubeSubscriptions.Any(ytsub => ytsub.Id == x.Id)))
                    {
                        _logger.LogInformation("Old subscription detected. Probably unsubscribed, removing subscription: {@oldsubscription}", oldSubscription);

                        context.Subscriptions.Remove(oldSubscription);
                    }

                    _logger.LogDebug("Saving updated subscription data");
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Saved updated subscription data");

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
                    _logger.LogError(e, "Update task unexpectedly threw an exception.");

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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("User subscriptions update task is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
