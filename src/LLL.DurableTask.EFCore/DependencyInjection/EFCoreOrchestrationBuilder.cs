using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.EFCore.DependencyInjection
{
    public class EFCoreOrchestrationBuilder : IEFCoreOrchestrationBuilder
    {
        public EFCoreOrchestrationBuilder(IServiceCollection services)
        {
            Services = services;
            DbContextConfigurations = new List<Action<DbContextOptionsBuilder>>();
        }

        public IServiceCollection Services { get; }

        public List<Action<DbContextOptionsBuilder>> DbContextConfigurations { get; }

        public IEFCoreOrchestrationBuilder ConfigureDbContext(Action<DbContextOptionsBuilder> configuration)
        {
            DbContextConfigurations.Add(configuration);
            return this;
        }
    }
}
