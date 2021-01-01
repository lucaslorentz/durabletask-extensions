using DurableTask.Core.Serializing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LLL.DurableTask.Core.Serializing
{
    public class UntypedJsonDataConverter : JsonDataConverter
    {
        public UntypedJsonDataConverter()
            : base(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Converters = {
                    new HistoryEventConverter(),
                    new StringEnumConverter()
                },
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            })
        {
        }
    }
}