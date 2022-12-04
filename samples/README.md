# Sample

This sample was built to demonstrate a microservices architecture with the following components:

- **Server:** Connects to storage and exposes it as GRPC endpoints.
- **Api:** Exposes REST API to manage orchestrations.
- **UI:** Exposes UI to manage orchestrations.
- **OrchestrationWorker:** Implements [BookParallel](samples/OrchestrationWorker/Orchestrations/BookParallelOrchestration.cs) and [BookSquential](samples/OrchestrationWorker/Orchestrations/BookSequentialOrchestration.cs) orchestrations for the given problem.
- **FlightWorker:** Implements [BookFlight](samples/FlightWorker/Activities/BookFlightActivity.cs) and [CancelFlight](samples/CarWorker/Activities/CancelFlightActivity.cs) activities.
- **CarWorker:** Implements [BookCar](samples/CarWorker/Activities/BookCarActivity.cs) and [CancelCar](samples/CarWorker/Activities/CancelCarActivity.cs) activities.
- **HotelWorker:** Implements [BookHotel](samples/HotelWorker/Activities/BookHotelActivity.cs) and [CancelHotel](samples/HotelWorker/Activities/CancelHotelActivity.cs) activities.
- **BPMNWorker:** An experimental BPMN runner built on top of Durable Tasks. There are also [BookParallel](samples/BpmnWorker/Workflows/BookParallel.bpmn) and [BookSequential](samples/BpmnWorker/Workflows/BookSequential.bpmn) BPMN workflows for the given problem.

## Runinng the sample

1. Configure a EFCore storage at the [server](samples/Server/Startup.cs#L37)
2. Simultaneously run all the projects listed above
3. Open the UI at https://localhost:5002/
4. Create the following test orchestrations and watch them be executed
   | Name | Version | InstanceId | Input |
   | --- | --- | --- | --- |
   | BookParallel | v1 | (Empty) | (Empty) |
   | BookSequential | v1 | (Empty) | (Empty) |
   | BPMN | (Empty) | (Empty) | { "name": "BookParallel" }
   | BPMN | (Empty) | (Empty) | { "name": "BookSequential" }
   | BPMN | (Empty) | (Empty) | { "name": "Bonus" }
