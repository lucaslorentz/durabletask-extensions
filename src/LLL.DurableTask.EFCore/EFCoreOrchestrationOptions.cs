using DurableTask.Core.Serializing;

namespace LLL.DurableTask.EFCore
{
    public class EFCoreOrchestrationOptions
    {
        public DataConverter DataConverter { get; set; } = new JsonDataConverter();
    }
}
