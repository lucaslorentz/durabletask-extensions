using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using Microsoft.Extensions.Logging;

namespace FlightWorker.Activities
{
    public class CancelFlightActivity : DistributedTaskActivity<CancelFlightInput, CancelFlightResult>
    {
        private readonly ILogger<CancelFlightActivity> _logger;

        public CancelFlightActivity(ILogger<CancelFlightActivity> logger)
        {
            _logger = logger;
        }

        protected override CancelFlightResult Execute(TaskContext context, CancelFlightInput input)
        {
            _logger.LogInformation("Canceling Flight {bookingId}", input.BookingId);

            return new CancelFlightResult();
        }
    }

    public class CancelFlightInput
    {
        public Guid BookingId { get; set; }
    }

    public class CancelFlightResult
    {
    }
}
