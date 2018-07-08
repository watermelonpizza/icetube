using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IceTube.DataModels
{
    public class YoutubeChannel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Sets the subscription as inactive, i.e. don't pull new feeds for this channel.
        /// Allows for non-subscription'ed channels to be added possibly in the future.
        /// </summary>
        public bool Inactive { get; set; }

        public DateTime LastCheckedAt { get; set; }

        public ICollection<YoutubeVideo> Videos { get; set; } = new List<YoutubeVideo>();
    }
}
