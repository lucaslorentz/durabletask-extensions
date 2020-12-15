﻿using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using Microsoft.Extensions.Logging;

namespace FlightWorker.Activities
{
    public class BookFlightActivity : DistributedTaskActivity<BookFlightInput, BookFlightResult>
    {
        private readonly ILogger<BookFlightActivity> _logger;

        public BookFlightActivity(ILogger<BookFlightActivity> logger)
        {
            _logger = logger;
        }

        protected override BookFlightResult Execute(TaskContext context, BookFlightInput input)
        {
            var bookingId = Guid.NewGuid();

            _logger.LogInformation("Booking Flight {bookingId}", bookingId);

            return new BookFlightResult
            {
                BookingId = bookingId
            };
        }
    }

    public class BookFlightInput
    {
    }

    public class BookFlightResult
    {
        public Guid BookingId { get; set; }
    }
}