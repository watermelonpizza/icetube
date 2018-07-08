using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IceTube.Google
{
    public class YoutubeActivity
    {
        public string Id { get; set; }
        public DateTime? PublishedAt { get; set; }

        public string ChannelId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ThumbnailUrl { get; set; }

        public string TypeRaw { get; set; }
        public YoutubeActivityType Type { get; set; }

        public string VideoId { get; set; }
    }
}
