using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LLL.DurableTask.EFCore.DependencyInjection;

public interface IEFCoreOrchestrationBuilder
{
    IServiceCollection Services { get; }

    IEFCoreOrchestrationBuilder ConfigureDbContext(Action<DbContextOptionsBuilder> options);
}
