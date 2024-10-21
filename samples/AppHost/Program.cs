var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.Server>("server");

builder.AddProject<Projects.Api>("api")
    .WithReference(server);

builder.AddProject<Projects.BpmnWorker>("bpmnworker")
    .WithReference(server);

builder.AddProject<Projects.CarWorker>("carworker")
    .WithReference(server);

builder.AddProject<Projects.FlightWorker>("flightworker")
    .WithReference(server);

builder.AddProject<Projects.HotelWorker>("hotelworker")
    .WithReference(server);

builder.AddProject<Projects.OrchestrationWorker>("orchestrationworker")
    .WithReference(server);

builder.AddProject<Projects.Ui>("ui")
    .WithReference(server);

builder.Build().Run();
