using System;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace FlightWorker.Activities;

[Activity(Name = "BookFlight", Version = "v1")]
public class BookFlightActivity : ActivityBase<BookFlightInput, BookFlightResult>
{
    private readonly ILogger<BookFlightActivity> _logger;

    public BookFlightActivity(ILogger<BookFlightActivity> logger)
    {
        _logger = logger;
    }

    public override Task<BookFlightResult> ExecuteAsync(BookFlightInput input)
    {
        var bookingId = Guid.NewGuid();

        _logger.LogInformation("Booking Flight {bookingId}", bookingId);

        return Task.FromResult(new BookFlightResult
        {
            BookingId = bookingId
        });
    }
}

public class BookFlightInput
{
}

public class BookFlightResult
{
    public Guid BookingId { get; set; }
}
