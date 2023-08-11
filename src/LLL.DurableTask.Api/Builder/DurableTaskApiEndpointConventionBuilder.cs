using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace LLL.DurableTask.Api;

public class DurableTaskApiEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _endpoints;

    public DurableTaskApiEndpointConventionBuilder()
    {
        _endpoints = new List<IEndpointConventionBuilder>();
    }

    public void AddEndpoints(IEnumerable<IEndpointConventionBuilder> endpoints)
    {
        _endpoints.AddRange(endpoints);
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var endpoint in _endpoints)
        {
            endpoint.Add(convention);
        }
    }
}
