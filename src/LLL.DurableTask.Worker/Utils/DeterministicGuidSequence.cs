using System;

namespace LLL.DurableTask.Worker.Utils
{
    public class DeterministicGuidSequence
    {
        private int _count = 0;

        public Guid NamespaceId { get; }

        public DeterministicGuidSequence(Guid namespaceId)
        {
            NamespaceId = namespaceId;
        }

        public Guid NewGuid()
        {
            return DeterministicGuid.Create(NamespaceId, (++_count).ToString());
        }
    }
}
