using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using static OrchestrationWorker.Orchestrations.BookParallelOrchestration;

namespace OrchestrationWorker.Orchestrations
{
    public class BookParallelOrchestration : DistributedTaskOrchestration<BookParallelResult, BookParallelInput>
    {
        public override async Task<BookParallelResult> RunTask(OrchestrationContext context, BookParallelInput input)
        {
            var compensations = new List<Func<Task>>();

            try
            {
                var tasks = new List<Task>();
                tasks.Add(new Func<Task>(async () =>
                {
                    var bookCarResult = await context.ScheduleTask<BookItemResult>("BookCar", "v1");
                    compensations.Add(() => context.ScheduleTask<CancelItemResult>("CancelCar", "v1", new
                    {
                        BookingId = bookCarResult.BookingId
                    }));
                })());

                tasks.Add(new Func<Task>(async () =>
                {
                    var bookHotelResult = await context.ScheduleTask<BookItemResult>("BookHotel", "v1");
                    compensations.Add(() => context.ScheduleTask<CancelItemResult>("CancelHotel", "v1", new
                    {
                        BookingId = bookHotelResult.BookingId
                    }));
                })());

                tasks.Add(new Func<Task>(async () =>
                {
                    var bookFlightResult = await context.ScheduleTask<BookItemResult>("BookFlight", "v1");
                    compensations.Add(() => context.ScheduleTask<CancelItemResult>("CancelFlight", "v1", new
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
