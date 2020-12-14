using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using Microsoft.Extensions.Logging;

namespace HotelWorker.Activities
{
    public class CancelHotelActivity : DistributedTaskActivity<CancelHotelInput, CancelHotelResult>
    {
        private readonly ILogger<CancelHotelActivity> _logger;

        public CancelHotelActivity(ILogger<CancelHotelActivity> logger)
        {
            _logger = logger;
        }

        protected override CancelHotelResult Execute(TaskContext context, CancelHotelInput input)
        {
            _logger.LogInformation("Canceling Hotel {bookingId}", input.BookingId);

            return new CancelHotelResult();
        }
    }

    public class CancelHotelInput
    {
        public Guid BookingId { get; set; }
    }

    public class CancelHotelResult
    {
    }
}
