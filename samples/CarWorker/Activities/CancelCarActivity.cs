using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace CarWorker.Activities
{
    [Activity(Name = "CancelCar", Version = "v1")]
    public class CancelCarActivity : DistributedTaskActivity<CancelCarInput, CancelCarResult>
    {
        private readonly ILogger<CancelCarActivity> _logger;

        public CancelCarActivity(ILogger<CancelCarActivity> logger)
        {
            _logger = logger;
        }

        protected override CancelCarResult Execute(TaskContext context, CancelCarInput input)
        {
            _logger.LogInformation("Canceling car {bookingId}", input.BookingId);

            return new CancelCarResult();
        }
    }

    public class CancelCarInput
    {
        public Guid BookingId { get; set; }
    }

    public class CancelCarResult
    {
    }
}
