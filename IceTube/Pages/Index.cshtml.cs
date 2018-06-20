using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using IceTube.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace IceTube.Pages
{
    public class IndexModel : PageModel
    {
        private readonly GoogleAccount _googleAccount;

        public IndexModel(GoogleAccount googleAccount)
        {
            _googleAccount = googleAccount;
        }

        public async Task OnGetAsync()
        {
            await _googleAccount.GetGoogleAccount();
        }
    }
}
