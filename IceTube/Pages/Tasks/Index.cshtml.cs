using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IceTube;
using IceTube.DataModels;

namespace IceTube.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<IceTubeTask> IceTubeTask { get;set; }

        public async Task OnGetAsync()
        {
            IceTubeTask = await _context.Tasks.ToListAsync();
        }
    }
}
