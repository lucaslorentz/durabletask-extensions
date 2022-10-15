#!/bin/bash -e

dotnet run --project samples/Server &
PIDS[0]=$!

dotnet run --project samples/Api &
PIDS[1]=$!

dotnet run --project samples/Ui &
PIDS[2]=$!

dotnet run --project samples/CarWorker &
PIDS[3]=$!

dotnet run --project samples/FlightWorker &
PIDS[4]=$!

dotnet run --project samples/HotelWorker &
PIDS[5]=$!

dotnet run --project samples/OrchestrationWorker &
PIDS[6]=$!

dotnet run --project samples/BpmnWorker &
PIDS[7]=$!

trap "kill ${PIDS[*]}" SIGINT

wait