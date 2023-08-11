using System;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace HotelWorker.Activities;

[Activity(Name = "BookHotel", Version = "v1")]
public class BookHotelActivity : ActivityBase<BookHotelInput, BookHotelResult>
{
    private readonly ILogger<BookHotelActivity> _logger;

    public BookHotelActivity(ILogger<BookHotelActivity> logger)
    {
        _logger = logger;
    }

    public override Task<BookHotelResult> ExecuteAsync(BookHotelInput input)
    {
        var bookingId = Guid.NewGuid();

        _logger.LogInformation("Booking Hotel {bookingId}", bookingId);

        return Task.FromResult(new BookHotelResult
        {
            BookingId = bookingId
        });
    }
}

public class BookHotelInput
{
}

public class BookHotelResult
{
    public Guid BookingId { get; set; }
}
