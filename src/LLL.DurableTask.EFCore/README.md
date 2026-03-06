# LLL.DurableTask.EFCore [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore)](https://www.nuget.org/packages/LLL.DurableTask.EFCore/)

LLL.DurableTask.EFCore provides relational database storage for Durable Task using Entity Framework Core (EFCore). This implementation leverages **row locking**, **skip locked**, and **polling** to create reliable and efficient task queues.

## Features

In addition to the standard features offered by the Durable Task framework, LLL.DurableTask.EFCore includes the following enhancements:

| Feature | Description |
| - | - |
| Worker specialization | Workers can optionally register only specific orchestrations and activities, allowing you to distribute tasks across multiple specialized services. These services connect to a shared database and collaboratively execute workflows. |
| Input/output observability | All inputs and outputs are stored as workflow events. This enhances visibility into the workflow's execution through the UI and supports a more robust rewind algorithm. |
| Enhanced rewind | We have implemented an advanced rewind algorithm that leverages stored inputs/outputs for more reliable replay. For more details, refer to [this comment](https://github.com/Azure/durabletask/issues/811#issuecomment-1324391970). |
| Orchestration tags | Orchestration tags are persisted and displayed in the UI, providing better workflow organization and tracking. |
| Full execution history | The state of each execution of a workflow instance is preserved, allowing you to view and inspect all previous executions in the UI. |
| Reliable event delivery | Completed orchestrations automatically reopen when they receive events, ensuring that no events are missed. This allows you to implement reliable orchestrations without needing to use "eternal orchestrations" via continue-as-new. More information is available at [this pull request](https://github.com/lucaslorentz/durabletask-extensions/pull/6). |
