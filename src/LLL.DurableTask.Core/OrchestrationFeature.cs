﻿namespace LLL.DurableTask.Core
{
    public enum OrchestrationFeature
    {
        SearchByInstanceId = 1,
        SearchByName = 2,
        SearchByCreatedTime = 3,
        SearchByStatus = 4,
        QueryCount = 5,
        Rewind = 6,
        Tags = 7,
        StatePerExecution = 8
    }
}
