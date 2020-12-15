﻿using System;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using Microsoft.Extensions.Logging;

namespace HotelWorker.Activities
{
    public class BookHotelActivity : DistributedTaskActivity<BookHotelInput, BookHotelResult>
    {
        private readonly ILogger<BookHotelActivity> _logger;

        public BookHotelActivity(ILogger<BookHotelActivity> logger)
        {
            _logger = logger;
        }

        protected override BookHotelResult Execute(TaskContext context, BookHotelInput input)
        {
            var bookingId = Guid.NewGuid();

            _logger.LogInformation("Booking Hotel {bookingId}", bookingId);

            return new BookHotelResult
            {
                BookingId = bookingId
            };
        }
    }

    public class BookHotelInput
    {
    }

    public class BookHotelResult
    {
        public Guid BookingId { get; set; }
    }
}