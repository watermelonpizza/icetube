﻿using System;
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

namespace IceTube.Pages.Subscriptions
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserSubscriptionsTask _subTask;

        public IndexModel(ApplicationDbContext context, UserSubscriptionsTask subTask)
        {
            _context = context;
            _subTask = subTask;
        }

        public IList<Subscription> Subscription { get;set; }

        public async Task OnGetAsync()
        {

            Subscription = await _context.Subscriptions.ToListAsync();
        }
    }
}
