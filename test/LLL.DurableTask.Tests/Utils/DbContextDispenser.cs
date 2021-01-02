using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLL.DurableTask.EFCore;

namespace LLL.DurableTask.Tests.Utils
{
    public class DbContextDispenser : IDisposable
    {
        private readonly Func<OrchestrationDbContext> _factory;
        private readonly OrchestrationDbContextExtensions _dbContextExtensions;
        private readonly Stack<IDisposable> _disposables;

        public DbContextDispenser(
            Func<OrchestrationDbContext> factory,
            OrchestrationDbContextExtensions dbContextExtensions)
        {
            _factory = factory;
            _dbContextExtensions = dbContextExtensions;
            _disposables = new Stack<IDisposable>();
        }

        public async Task<OrchestrationDbContext> Get()
        {
            var dbContext = _factory();
            var transaction = await _dbContextExtensions.BeginTransaction(dbContext);
            return dbContext;
        }

        public void Dispose()
        {
            while (_disposables.TryPop(out var disposable))
                disposable.Dispose();
        }
    }
}