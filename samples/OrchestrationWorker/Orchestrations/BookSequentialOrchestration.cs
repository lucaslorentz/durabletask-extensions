using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using static OrchestrationWorker.Orchestrations.BookSequentialOrchestration;

namespace OrchestrationWorker.Orchestrations;

[Orchestration(Name = "BookSequential", Version = "v1")]
public class BookSequentialOrchestration : OrchestrationBase<BookSequentialResult, BookSequentialInput>
{
    public override async Task<BookSequentialResult> Execute(BookSequentialInput input)
    {
        var compensations = new List<Func<Task>>();

        try
        {
            var bookCarResult = await Context.ScheduleTask<BookItemResult>("BookCar", "v1");
            compensations.Add(() => Context.ScheduleTask<CancelItemResult>("CancelCar", "v1", new
            {
                bookCarResult.BookingId
            }));

            var bookHotelResult = await Context.ScheduleTask<BookItemResult>("BookHotel", "v1");
            compensations.Add(() => Context.ScheduleTask<CancelItemResult>("CancelHotel", "v1", new
            {
                bookHotelResult.BookingId
            }));

            var bookFlightResult = await Context.ScheduleTask<BookItemResult>("BookFlight", "v1");
            compensations.Add(() => Context.ScheduleTask<CancelItemResult>("CancelFlight", "v1", new
            {
                bookFlightResult.BookingId
            }));

            throw new Exception("Something failed");
        }
        catch
        {
            foreach (var compensation in compensations)
            {
                await compensation();
            }
        }

        return new BookSequentialResult();
    }

    public class BookSequentialInput
    {
    }

    public class BookSequentialResult
    {
    }

    public class BookItemResult
    {
        public Guid BookingId { get; set; }
    }

    public class CancelItemResult
    {
    }
}
