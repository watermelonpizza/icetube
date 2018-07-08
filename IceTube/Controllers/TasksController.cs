using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IceTube.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        private readonly YoutubeChannelFeedTask _youtubeChannelFeedTask;

        public TasksController(
            ILogger<TasksController> logger, 
            ApplicationDbContext context,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _context = context;

            var services = serviceProvider.GetServices<IHostedService>();

            _userSubscriptionsTask = services.OfType<UserSubscriptionsTask>().Single();
            _youtubeChannelFeedTask = services.OfType<YoutubeChannelFeedTask>().Single();
        }

        [HttpPost]
        [Route("update")]
        public IActionResult UpdateTaskNow(string task)
        {
            if (task == UserSubscriptionsTask.UserSubscriptionsTaskName)
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
            else if (task == YoutubeChannelFeedTask.YoutubeChannelFeedTaskName)
            {
                if (_youtubeChannelFeedTask.QueueNow(HttpContext.RequestAborted, out string error))
                {
                    return Ok("Updating channel feeds");
                }
                else
                {
                    return Conflict($"Could not run channel feed update task right now. Error: {error}");
                }
            }

            return NotFound("A task by that name wasn't found");
        }
    }
}