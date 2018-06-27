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

namespace IceTube.Google
{
    public class GoogleAccount
    {
        private const string DataStoreKey = "user";
        private const string GoogleUserCredentialsMemoryKey = "GoogleCredentials";

        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IDataStore _dataStore;

        public GoogleAccount(
            IMemoryCache memoryCache,
            IConfiguration configuration,
            ApplicationDbContext context,
            IDataStore dataStore)
        {
            _cache = memoryCache;
            _configuration = configuration;
            _context = context;
            _dataStore = dataStore;
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

        public async Task<List<GoogleSubscription>> GetSubscriptionsAsync()
        {
            var creds = await GetGoogleUserCredentials();

            var youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = creds,
                    ApplicationName = "IceTube"
                });


            List<GoogleSubscription> results = new List<GoogleSubscription>();

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
                        x => new GoogleSubscription
                        {
                            Id = x.Snippet.ResourceId.ChannelId,
                            Name = x.Snippet.Title,
                            Description = x.Snippet.Description
                        }));

                nextPageToken = result.NextPageToken;
            }

            return results;
        }
    }
}
