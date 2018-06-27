using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IceTube.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IceTube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ILogger<TasksController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserSubscriptionsTask _userSubscriptionsTask;

        public TasksController(ILogger<TasksController> logger, ApplicationDbContext context, UserSubscriptionsTask userSubscriptionsTask)
        {
            _logger = logger;
            _context = context;
            _userSubscriptionsTask = userSubscriptionsTask;
        }

        [HttpPost]
        [Route("update-subscriptions")]
        public IActionResult UpdateSubscriptions()
        {
            if (_userSubscriptionsTask.QueueNow(HttpContext.RequestAborted, out string error))
            {
                return Ok("Updating subscriptions");
            }
            else
            {
                return Conflict($"Could not run update subscriptions task right now. Error: {error}");
            }
        }
    }
}