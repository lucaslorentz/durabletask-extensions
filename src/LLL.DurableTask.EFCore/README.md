# LLL.DurableTask.EFCore [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore)](https://www.nuget.org/packages/LLL.DurableTask.EFCore/)

LLL.DurableTask.EFCore provides relational database storage for Durable Task using Entity Framework Core (EFCore). This implementation leverages **row locking**, **skip locked**, and **polling** to create reliable and efficient task queues.

## Features

In addition to the standard features offered by the Durable Task framework, LLL.DurableTask.EFCore includes the following enhancements:

| Feature | Description |
| - | - | 
| Distributed workers | Distribute your orchestrations and activities across multiple microservices, each dedicated to specific types of tasks. These microservices connect to a shared database and collaboratively execute tasks to complete workflows efficiently and reliably. |
| Store all inputs/outputs | All inputs and outputs are stored as workflow events. This enhances visibility into the workflow's execution through the UI and supports a more robust rewind algorithm. |
| Improved rewind | We have implemented an advanced rewind algorithm. For more details, refer to [this comment](https://github.com/Azure/durabletask/issues/811#issuecomment-1324391970). |
| Tags | Orchestration tags are persisted and displayed in the UI, providing better workflow organization and tracking. |
| State per execution | The state of each execution of a workflow instance is preserved, allowing you to view all executions in the UI. |
| Guaranteed Event Delivery | Orchestrations reopen when they receive events, ensuring that no events are missed. This allows you to implement reliable orchestrations without needing to use "eternal orchestrations" via continue-as-new. More information is available at [this pull request](https://github.com/lucaslorentz/durabletask-extensions/pull/6). |
