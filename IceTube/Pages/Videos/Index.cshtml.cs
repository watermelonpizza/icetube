using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IceTube;
using IceTube.DataModels;
using Microsoft.Extensions.Hosting;

namespace IceTube.Pages.Videos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public YoutubeChannel Channel { get; set; }

        public IList<YoutubeVideo> YoutubeVideos { get; set; } = new List<YoutubeVideo>();

        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(string channelId = null)
        {
            if (!string.IsNullOrEmpty(channelId))
            {
                Channel = await _context.Channels.FindAsync(channelId);

                if (Channel == null)
                {
                    ErrorMessage = $"Invalid channel id: {channelId} given";
                    return;
                }

                YoutubeVideos = await _context.Videos.Where(x => x.ChannelId == channelId).ToListAsync();
            }
            else
            {
                YoutubeVideos = await _context.Videos.Include(y => y.Channel).ToListAsync();
            }
        }
    }
}
