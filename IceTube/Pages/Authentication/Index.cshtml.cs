using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IceTube.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IceTube.Pages.Authentication
{
    public class IndexModel : PageModel
    {
        private readonly GoogleAccount _googleAccount;

        public string Username { get; set; }

        public bool HasSetupGoogleAccount { get; set; }

        public IndexModel(GoogleAccount googleAccount)
        {
            _googleAccount = googleAccount;
        }

        public async Task OnGetAsync()
        {
            var credential = await _googleAccount.GetGoogleUserCredentials();

            Username = credential.UserId;
        }
    }
}