using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using Microsoft.Extensions.Logging;

namespace CarWorker.Activities
{
    public class BookCarActivity : DistributedTaskActivity<BookCarInput, BookCarResult>
    {
        private readonly ILogger<BookCarActivity> _logger;

        public BookCarActivity(ILogger<BookCarActivity> logger)
        {
            _logger = logger;
        }

        protected override BookCarResult Execute(TaskContext context, BookCarInput input)
        {
            var bookingId = Guid.NewGuid();

            _logger.LogInformation("Booking car {bookingId}", bookingId);

            return new BookCarResult
            {
                BookingId = bookingId
            };
        }
    }

    public class BookCarInput
    {
    }

    public class BookCarResult
    {
        public Guid BookingId { get; set; }
    }
}
