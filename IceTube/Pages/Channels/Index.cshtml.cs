using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IceTube;
using IceTube.DataModels;
using Microsoft.Extensions.DependencyInjection;
using IceTube.Tasks;

namespace IceTube.Pages.Channels
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<YoutubeChannel> Subscription { get;set; }

        public async Task OnGetAsync()
        {
            Subscription = await _context.Channels.ToListAsync();
        }
    }
}
