using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.EntityFrameworkCore;

namespace IceTube.Google
{
    public class GoogleAccount
    {
        private const string DataStoreKey = "user";

        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IDataStore _dataStore;

        public GoogleAccount(
            IConfiguration configuration,
            ApplicationDbContext context,
            IDataStore dataStore)
        {
            _configuration = configuration;
            _context = context;
            _dataStore = dataStore;
        }

        public async Task<bool> HasSetup()
        {
            return await _context.GoogleDataStores.AnyAsync();
        }

        public async Task GetGoogleAccount()
        {
            UserCredential credential;
            var secrets = new ClientSecrets
            {
                ClientId = _configuration["Google:ClientId"],
                ClientSecret = _configuration["Google:ClientSecret"]
            };

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                // This OAuth 2.0 access scope allows for read-only access to the authenticated 
                // user's account, but not other types of account access.
                new[] { YouTubeService.Scope.YoutubeReadonly },
                "user",
                CancellationToken.None,
                _dataStore
            );

            var youtubeService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "IceTube"
                });

            var subs = youtubeService.Subscriptions.List("id");

            var channelsListRequest = youtubeService.Channels.List("contentDetails");
            channelsListRequest.Mine = true;

            // Retrieve the contentDetails part of the channel resource for the authenticated user's channel.
            var channelsListResponse = await channelsListRequest.ExecuteAsync();

            foreach (var channel in channelsListResponse.Items)
            {
                // From the API response, extract the playlist ID that identifies the list
                // of videos uploaded to the authenticated user's channel.
                var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;

                Console.WriteLine("Videos in list {0}", uploadsListId);

                var nextPageToken = "";
                while (nextPageToken != null)
                {
                    var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                    playlistItemsListRequest.PlaylistId = uploadsListId;
                    playlistItemsListRequest.MaxResults = 50;
                    playlistItemsListRequest.PageToken = nextPageToken;

                    // Retrieve the list of videos uploaded to the authenticated user's channel.
                    var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                    foreach (var playlistItem in playlistItemsListResponse.Items)
                    {
                        // Print information about each video.
                        Console.WriteLine("{0} ({1})", playlistItem.Snippet.Title, playlistItem.Snippet.ResourceId.VideoId);
                    }

                    nextPageToken = playlistItemsListResponse.NextPageToken;
                }
            }
        }
    }
}
