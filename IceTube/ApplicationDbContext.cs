using IceTube.Google;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IceTube
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<GoogleDataStoreObject> GoogleDataStores { get; set; }
    }
}
