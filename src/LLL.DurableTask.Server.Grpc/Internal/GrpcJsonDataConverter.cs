using System;
using DurableTask.Core.Serializing;

namespace LLL.DurableTask.Server.Grpc.Server.Internal
{
    class GrpcJsonDataConverter : JsonDataConverter
    {
        public override string Serialize(object value, bool formatted)
        {
            if (value == null)
                return string.Empty;

            return base.Serialize(value, formatted);
        }

        public override object Deserialize(string data, Type objectType)
        {
            if (string.IsNullOrEmpty(data))
                return default;

            return base.Deserialize(data, objectType);
        }
    }
}
