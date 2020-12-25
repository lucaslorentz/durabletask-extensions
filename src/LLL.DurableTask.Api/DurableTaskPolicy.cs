namespace LLL.DurableTask.Api
{
    public static class DurableTaskPolicy
    {
        public static string Read = "DurableTaskRead";
        public static string Create = "DurableTaskCreate";
        public static string Terminate = "DurableTaskTerminate";
        public static string RaiseEvent = "DurableTaskRaiseEvent";
        public static string Purge = "DurableTaskPurge";
    }
}