using System;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;

namespace CarWorker.Activities;

[Activity(Name = "CancelCar", Version = "v1")]
public class CancelCarActivity : ActivityBase<CancelCarInput, CancelCarResult>
{
    private readonly ILogger<CancelCarActivity> _logger;

    public CancelCarActivity(ILogger<CancelCarActivity> logger)
    {
        _logger = logger;
    }

    public override Task<CancelCarResult> ExecuteAsync(CancelCarInput input)
    {
        _logger.LogInformation("Canceling car {bookingId}", input.BookingId);

        return Task.FromResult(new CancelCarResult());
    }
}

public class CancelCarInput
{
    public Guid BookingId { get; set; }
}

public class CancelCarResult
{
}
