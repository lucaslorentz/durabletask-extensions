using System;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace FlightWorker.Activities
{
    [Activity(Name = "CancelFlight", Version = "v1")]
    public class CancelFlightActivity : ActivityBase<CancelFlightInput, CancelFlightResult>
    {
        private readonly ILogger<CancelFlightActivity> _logger;

        public CancelFlightActivity(ILogger<CancelFlightActivity> logger)
        {
            _logger = logger;
        }

        public override Task<CancelFlightResult> ExecuteAsync(CancelFlightInput input)
        {
            _logger.LogInformation("Canceling Flight {bookingId}", input.BookingId);

            return Task.FromResult(new CancelFlightResult());
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
