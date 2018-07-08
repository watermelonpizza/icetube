using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using IceTube.DataModels;
using Microsoft.Extensions.Logging;

namespace IceTube.Google
{
    public class GoogleAccount : IDisposable
    {
        private const string DataStoreKey = "user";
        private const string GoogleUserCredentialsMemoryKey = "GoogleCredentials";

        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IDataStore _dataStore;
        private readonly ILogger<GoogleAccount> _logger;

        public GoogleAccount(
            IMemoryCache memoryCache,
            IConfiguration configuration,
            ApplicationDbContext context,
            IDataStore dataStore,
            ILogger<GoogleAccount> logger)
        {
            _cache = memoryCache;
            _configuration = configuration;
            _context = context;
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<bool> HasSetup()
        {
            return await _context.GoogleDataStores.AnyAsync();
        }

        public async Task<UserCredential> GetGoogleUserCredentials()
        {
            UserCredential credential =
                await _cache.GetOrCreateAsync(
                    GoogleUserCredentialsMemoryKey,
                    async entry =>
                    {
                        var secrets = new ClientSecrets
                        {
                            ClientId = _configuration["Google:ClientId"],
                            ClientSecret = _configuration["Google:ClientSecret"]
                        };

                        entry.SetSlidingExpiration(TimeSpan.FromDays(1));

                        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                            secrets,
                            // This OAuth 2.0 access scope allows for read-only access to the authenticated 
                            // user's account, but not other types of account access.
                            new[] { YouTubeService.Scope.YoutubeReadonly },
                            "user",
                            CancellationToken.None,
                            _dataStore);
                    }
                );

            return credential;
        }

        public async Task<List<Channel>> GetSubscriptionsAsync()
        {
            var creds = await GetGoogleUserCredentials();

            var youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = creds,
                    ApplicationName = "IceTube"
                });


            List<Channel> results = new List<Channel>();

            var nextPageToken = "";
            while (nextPageToken != null)
            {
                var subs = youtubeService.Subscriptions.List("snippet");
                subs.Mine = true;
                subs.MaxResults = 50;
                subs.PageToken = nextPageToken;

                var result = await subs.ExecuteAsync();

                results.AddRange(
                    result.Items.Select(
                        x => new Channel
                        {
                            Id = x.Snippet.ResourceId.ChannelId,
                            Name = x.Snippet.Title,
                            Description = x.Snippet.Description
                        }));

                nextPageToken = result.NextPageToken;
            }

            return results;
        }

        public async Task<List<YoutubeActivity>> GetChannelActivityAsync(YoutubeChannel channel)
        {
            var creds = await GetGoogleUserCredentials();

            var youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = creds,
                    ApplicationName = "IceTube"
                });

            List<YoutubeActivity> results = new List<YoutubeActivity>();

            if (!int.TryParse(_configuration["ApiLimits:MaximumActivityResults"], out int maxResultsAllowed))
            {
                maxResultsAllowed = 200;
            }

            var nextPageToken = "";
            while (nextPageToken != null && results.Count < maxResultsAllowed)
            {
                var activities = youtubeService.Activities.List("snippet,contentDetails");
                activities.MaxResults = 50;
                activities.PublishedAfter = channel.LastCheckedAt;
                activities.ChannelId = channel.Id;
                activities.PageToken = nextPageToken;

                var result = await activities.ExecuteAsync();

                results.AddRange(
                    result.Items.Select(
                        x =>
                        {
                            if (!Enum.TryParse(x.Snippet.Type, true, out YoutubeActivityType type))
                            {
                                type = YoutubeActivityType.Unknown;
                            }

                            return new YoutubeActivity
                            {
                                Id = x.Id,
                                ChannelId = x.Snippet.ChannelId,
                                Title = x.Snippet.Title,
                                Description = x.Snippet.Description,
                                Type = type,
                                TypeRaw = x.Snippet.Type,
                                PublishedAt = x.Snippet.PublishedAt,
                                ThumbnailUrl = x.Snippet.Thumbnails?.Maxres?.Url,
                                VideoId = x.ContentDetails?.Upload?.VideoId
                            };
                        }));

                nextPageToken = result.NextPageToken;
            }

            if (nextPageToken != null && results.Count > maxResultsAllowed)
            {
                _logger.LogWarning("Hit maximum activity download limit while getting results for channel: {channelName}, published after: {publishedAfterDate}. This will mean some uploads may have been missed. Increase the MaximumActivityResults config value or decrease the youtube channel feed update task interval to prevent this.", channel);
            }

            return results;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
