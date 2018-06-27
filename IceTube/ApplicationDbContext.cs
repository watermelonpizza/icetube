using IceTube.DataModels;
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
        public ApplicationDbContext(DbContextOptions options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GoogleDataStoreObject>(entity =>
            {
                entity.HasKey(x => x.Key);
            });

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Internal google data store objects, used for tracking login information used by the google api
        /// </summary>
        public DbSet<GoogleDataStoreObject> GoogleDataStores { get; set; }

        /// <summary>
        /// The users youtube subscriptions
        /// </summary>
        public DbSet<Subscription> Subscriptions { get; set; }

        /// <summary>
        /// Information about all the tasks in the system, mostly to track last ran and success status
        /// </summary>
        public DbSet<IceTubeTask> Tasks { get; set; }
    }
}
