using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LLL.DurableTask.EFCore.Configuration.Converters
{
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(To, From)
        {
        }

        static readonly Expression<Func<DateTime, DateTime>> To = x => x.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(x, DateTimeKind.Utc)
                : x.ToUniversalTime();

        static readonly Expression<Func<DateTime, DateTime>> From = x => DateTime.SpecifyKind(x, DateTimeKind.Utc);
    }
}
