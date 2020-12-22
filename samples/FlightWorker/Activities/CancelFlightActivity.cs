using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace FlightWorker.Activities
{
    [Activity(Name = "CancelFlight", Version = "v1")]
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
