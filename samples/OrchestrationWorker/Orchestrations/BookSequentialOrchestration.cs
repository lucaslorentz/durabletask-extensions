using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using static OrchestrationWorker.Orchestrations.BookSequentialOrchestration;

namespace OrchestrationWorker.Orchestrations
{
    [Orchestration(Name = "BookSequential", Version = "v1")]
    public class BookSequentialOrchestration : DistributedTaskOrchestration<BookSequentialResult, BookSequentialInput>
    {
        public override async Task<BookSequentialResult> RunTask(OrchestrationContext context, BookSequentialInput input)
        {
            var compensations = new List<Func<Task>>();

            try
            {
                var bookCarResult = await context.ScheduleTask<BookItemResult>("BookCar", "v1");
                compensations.Add(() => context.ScheduleTask<CancelItemResult>("CancelCar", "v1", new
                {
                    BookingId = bookCarResult.BookingId
                }));

                var bookHotelResult = await context.ScheduleTask<BookItemResult>("BookHotel", "v1");
                compensations.Add(() => context.ScheduleTask<CancelItemResult>("CancelHotel", "v1", new
                {
                    BookingId = bookHotelResult.BookingId
                }));

                var bookFlightResult = await context.ScheduleTask<BookItemResult>("BookFlight", "v1");
                compensations.Add(() => context.ScheduleTask<CancelItemResult>("CancelFlight", "v1", new
                {
                    BookingId = bookFlightResult.BookingId
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
}
