using DurableTask.Core.Serializing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LLL.DurableTask.Core.Serializing
{
    public class TypelessJsonDataConverter : JsonDataConverter
    {
        public TypelessJsonDataConverter()
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
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            })
        {
        }
    }
}