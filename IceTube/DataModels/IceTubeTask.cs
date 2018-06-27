using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IceTube.DataModels
{
    public class IceTubeTask
    {
        [Key]
        public string TaskName { get; set; }

        public DateTime? LastRan { get; set; }

        public bool? LastRanSuccess { get; set; }

        public string LastRanStatus { get; set; }
    }
}
