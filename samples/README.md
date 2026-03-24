# Sample

This sample was built to demonstrate a microservices architecture with the following components:

- **Server:** Connects to storage and exposes it as GRPC endpoints.
- **Api:** Exposes REST API to manage orchestrations.
- **UI:** Exposes UI to manage orchestrations.
- **OrchestrationWorker:** Implements [BookParallel](OrchestrationWorker/Orchestrations/BookParallelOrchestration.cs), [BookSequential](OrchestrationWorker/Orchestrations/BookSequentialOrchestration.cs), [EventDemo](OrchestrationWorker/Orchestrations/EventDemoOrchestration.cs), and [RewindDemo](OrchestrationWorker/Orchestrations/RewindDemoOrchestration.cs) orchestrations for the given problem.
- **FlightWorker:** Implements [BookFlight](FlightWorker/Activities/BookFlightActivity.cs) and [CancelFlight](CarWorker/Activities/CancelFlightActivity.cs) activities.
- **CarWorker:** Implements [BookCar](CarWorker/Activities/BookCarActivity.cs) and [CancelCar](CarWorker/Activities/CancelCarActivity.cs) activities.
- **HotelWorker:** Implements [BookHotel](HotelWorker/Activities/BookHotelActivity.cs) and [CancelHotel](HotelWorker/Activities/CancelHotelActivity.cs) activities.
- **BPMNWorker:** An experimental BPMN runner built on top of Durable Tasks. There are also [BookParallel](BpmnWorker/Workflows/BookParallel.bpmn) and [BookSequential](BpmnWorker/Workflows/BookSequential.bpmn) BPMN workflows for the given problem.
- **AppHost:** Aspire.NET host project to orchestrate and run all applications.

## Running the sample

1. Configure a EFCore storage at the [server](Server/Program.cs#L9)
1. Run the [AppHost project](AppHost) with dotnet run.
1. Open Aspire Dashboard at https://localhost:17198/
1. Open the DurableTask UI at https://localhost:5002/
1. Create the following test orchestrations and watch them be executed:

   | Name | Version | InstanceId | Input
   | --- | --- | --- | ---
   | BookParallel | v1 | (Empty) | {}
   | BookSequential | v1 | (Empty) | {}
   | EventDemo | v1 | (Empty) | { "correlationId": "demo-1" }
   | RewindDemo | v1 | (Empty) | { "RequestedCarType": "Sports" }
   | BPMN | (Empty) | (Empty) | { "name": "BookParallel" }
   | BPMN | (Empty) | (Empty) | { "name": "BookSequential" }
   | BPMN | (Empty) | (Empty) | { "name": "Bonus" }

1. To test external events with `EventDemo`, raise these events to the running instance:

   | Event Name | Event Data
   | --- | ---
   | ApprovalRequested | { "approved": true, "approvedBy": "alice", "reason": "looks good" }
   | AddComment | { "text": "approved from UI" }

1. To test rewind with `RewindDemo`:

   1. Start `RewindDemo` with version `v1` and input `{ "RequestedCarType": "Sports" }`.
   1. It fails intentionally on the first execution.
   1. Open the instance and click **Rewind**.
   1. The rewound execution succeeds.
