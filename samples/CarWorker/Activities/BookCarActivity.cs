using System;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace CarWorker.Activities;

[Activity(Name = "BookCar", Version = "v1")]
public class BookCarActivity : ActivityBase<BookCarInput, BookCarResult>
{
    private readonly ILogger<BookCarActivity> _logger;

    public BookCarActivity(ILogger<BookCarActivity> logger)
    {
        _logger = logger;
    }

    public override Task<BookCarResult> ExecuteAsync(BookCarInput input)
    {
        var bookingId = Guid.NewGuid();

        _logger.LogInformation("Booking car {bookingId}", bookingId);

        return Task.FromResult(new BookCarResult
        {
            BookingId = bookingId
        });
    }
}

public class BookCarInput
{
}

public class BookCarResult
{
    public Guid BookingId { get; set; }
}
