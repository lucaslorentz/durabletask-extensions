using System;
using System.Collections.Generic;
using DurableTask.Core;

namespace LLL.DurableTask.Core;

public class PurgeInstanceFilterExtended : PurgeInstanceFilter
{
    public PurgeInstanceFilterExtended(
        DateTime createdTimeFrom,
        DateTime? createdTimeTo,
        IEnumerable<OrchestrationStatus> runtimeStatus)
        : base(createdTimeFrom, createdTimeTo, runtimeStatus)
    {
    }

    public int? Limit { get; set; }
}
