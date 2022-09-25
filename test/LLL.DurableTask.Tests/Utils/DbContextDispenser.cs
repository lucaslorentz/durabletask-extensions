using System;
using System.Collections.Generic;
using LLL.DurableTask.EFCore;
using Microsoft.EntityFrameworkCore;

namespace LLL.DurableTask.Tests.Utils
{
    public class DbContextDispenser : IDisposable
    {
        private readonly IDbContextFactory<OrchestrationDbContext> _factory;
        private readonly Stack<IDisposable> _disposables;

        public DbContextDispenser(IDbContextFactory<OrchestrationDbContext> factory)
        {
            _factory = factory;
            _disposables = new Stack<IDisposable>();
        }

        public OrchestrationDbContext Get()
        {
            return _factory.CreateDbContext();
        }

        public void Dispose()
        {
            while (_disposables.TryPop(out var disposable))
                disposable.Dispose();
        }
    }
}