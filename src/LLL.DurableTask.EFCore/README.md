# LLL.DurableTask.EFCore [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.EFCore)](https://www.nuget.org/packages/LLL.DurableTask.EFCore/)

Relational database storage using EFCore.

The implementation uses a combination of **row locking**, **skip locked** and **polling** to implement queues.

## Features

Besides all features defined by Durable Task framework, this storage also supports the extra features described below.

| Feature | Description |
| - | - | 
| Distributed workers | Split your orchestation and activities into multiple micro-services, they all connect to same database and cooperatively executes all tasks to complete workflows. |
| Store all inputs/outputs | All inputs and outputs are stored as workflow events, this makes it easier to understand the flow using the UI, and also allow us to do better rewind algorithm. |
| Improved rewind | We've implemented a top notch rewind algorithm. More info in [this comment](https://github.com/Azure/durabletask/issues/811#issuecomment-1324391970) |
| Tags | Orhcestration tags are persisted and can be seen in UI |
| State per execution | We keep the state of all executions of a workflow instance and you can view all executions in the UI |
| Unmissable events | Reopen orchestrations when they receive events. This allows you to implement orchestrations that never miss events without having to implement "eternal orchestrations" using continue as new. More info at https://github.com/lucaslorentz/durabletask-extensions/pull/6 |
