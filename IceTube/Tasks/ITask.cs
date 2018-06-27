using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IceTube.Tasks
{
    interface ITask
    {
        /// <summary>
        /// Queues up the task to run asap ignoring the previously set run date and recreates the timers.
        /// Function will return <c>false</c> if the task is already running.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token usually provided through the controller.</param>
        /// <param name="error">Error message if returned <c>false</c></param>
        /// <returns>Whether or not the queue was succesfull</returns>
        bool QueueNow(CancellationToken cancellationToken, out string error);
    }
}
