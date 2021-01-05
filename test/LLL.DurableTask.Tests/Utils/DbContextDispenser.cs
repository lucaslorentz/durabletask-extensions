using System;
using System.Collections.Generic;
using LLL.DurableTask.EFCore;

namespace LLL.DurableTask.Tests.Utils
{
    public class DbContextDispenser : IDisposable
    {
        private readonly Func<OrchestrationDbContext> _factory;
        private readonly Stack<IDisposable> _disposables;

        public DbContextDispenser(Func<OrchestrationDbContext> factory)
        {
            _factory = factory;
            _disposables = new Stack<IDisposable>();
        }

        public OrchestrationDbContext Get()
        {
            return _factory();
        }

        public void Dispose()
        {
            while (_disposables.TryPop(out var disposable))
                disposable.Dispose();
        }
    }
}