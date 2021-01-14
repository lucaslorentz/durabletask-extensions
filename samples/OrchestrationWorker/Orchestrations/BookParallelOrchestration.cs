using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using static OrchestrationWorker.Orchestrations.BookParallelOrchestration;

namespace OrchestrationWorker.Orchestrations
{
    [Orchestration(Name = "BookParallel", Version = "v1")]
    public class BookParallelOrchestration : OrchestrationBase<BookParallelResult, BookParallelInput>
    {
        public override async Task<BookParallelResult> Execute(BookParallelInput input)
        {
            var compensations = new List<Func<Task>>();

            try
            {
                var tasks = new List<Task>();
                tasks.Add(new Func<Task>(async () =>
                {
                    var bookCarResult = await Context.ScheduleTask<BookItemResult>("BookCar", "v1");
                    compensations.Add(() => Context.ScheduleTask<CancelItemResult>("CancelCar", "v1", new
                    {
                        BookingId = bookCarResult.BookingId
                    }));
                })());

                tasks.Add(new Func<Task>(async () =>
                {
                    var bookHotelResult = await Context.ScheduleTask<BookItemResult>("BookHotel", "v1");
                    compensations.Add(() => Context.ScheduleTask<CancelItemResult>("CancelHotel", "v1", new
                    {
                        BookingId = bookHotelResult.BookingId
                    }));
                })());

                tasks.Add(new Func<Task>(async () =>
                {
                    var bookFlightResult = await Context.ScheduleTask<BookItemResult>("BookFlight", "v1");
                    compensations.Add(() => Context.ScheduleTask<CancelItemResult>("CancelFlight", "v1", new
                    {
                        BookingId = bookFlightResult.BookingId
                    }));
                })());

                await Task.WhenAll(tasks);

                throw new Exception("Something failed");
            }
            catch
            {
                await Task.WhenAll(compensations.Select(c => c()));
            }

            return new BookParallelResult();
        }

        public class BookParallelInput
        {
        }

        public class BookParallelResult
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
}
