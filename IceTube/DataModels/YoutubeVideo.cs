using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IceTube.DataModels
{
    public class YoutubeVideo
    {
        public int Id { get; set; }

        public string ActivityId { get; set; }

        public string VideoId { get; set; }

        public DateTime? PublishedAt { get; set; }

        public DateTime AddedAt { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ThumbnailUrl { get; set; }

        public VideoDownloadState DownloadState { get; set; }

        public DateTime? StartedDownloadAt { get; set; }
        public DateTime? FinishedDownloadAt { get; set; }

        public bool DownloadError { get; set; }
        public string DownloadErrorDetails { get; set; }

        public string ChannelId { get; set; }
        public YoutubeChannel Channel { get; set; }
    }
}