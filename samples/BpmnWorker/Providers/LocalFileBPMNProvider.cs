namespace BpmnWorker.Providers;

public class LocalFileBPMNProvider : IBPMNProvider
{
    public Task<byte[]> GetBPMN(string name)
    {
        var bpmnPath = $"Workflows/{name}.bpmn";

        var bytes = File.ReadAllBytes(bpmnPath);

        return Task.FromResult(bytes);
    }
}
