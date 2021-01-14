using System;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace HotelWorker.Activities
{
    [Activity(Name = "CancelHotel", Version = "v1")]
    public class CancelHotelActivity : ActivityBase<CancelHotelInput, CancelHotelResult>
    {
        private readonly ILogger<CancelHotelActivity> _logger;

        public CancelHotelActivity(ILogger<CancelHotelActivity> logger)
        {
            _logger = logger;
        }

        public override Task<CancelHotelResult> ExecuteAsync(CancelHotelInput input)
        {
            _logger.LogInformation("Canceling Hotel {bookingId}", input.BookingId);

            return Task.FromResult(new CancelHotelResult());
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
