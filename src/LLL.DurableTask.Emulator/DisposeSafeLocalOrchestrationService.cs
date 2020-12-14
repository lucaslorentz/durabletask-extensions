using System;
using DurableTask.Emulator;

namespace LLL.DurableTask.Emulator
{
    public class DisposeSafeLocalOrchestrationService : LocalOrchestrationService, IDisposable
    {
        private bool _disposed = false;

        public new void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            base.Dispose();
        }
    }
}
