using System.Threading.Tasks;

namespace BpmnWorker.Providers
{
    public interface IBPMNProvider
    {
        public Task<byte[]> GetBPMN(string name);
    }
}
