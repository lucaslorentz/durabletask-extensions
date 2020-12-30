namespace LLL.DurableTask.Api
{
    public static class DurableTaskPolicy
    {
        public const string Entrypoint = "DurableTaskEntrypoint";
        public const string Read = "DurableTaskRead";
        public const string ReadHistory = "DurableTaskReadHistory";
        public const string Create = "DurableTaskCreate";
        public const string Terminate = "DurableTaskTerminate";
        public const string Rewind = "DurableTaskRewind";
        public const string RaiseEvent = "DurableTaskRaiseEvent";
        public const string Purge = "DurableTaskPurge";
    }
}