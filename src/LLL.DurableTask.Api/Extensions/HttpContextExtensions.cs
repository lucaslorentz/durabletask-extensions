using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LLL.DurableTask.Api.Extensions
{
    static class HttpContextExtensions
    {
        private static JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Converters = { new StringEnumConverter() }
        };

        public static T ParseQuery<T>(this HttpContext context)
            where T : new()
        {
            var obj = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (context.Request.Query.TryGetValue(property.Name, out var values))
                {
                    if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        var elementType = property.PropertyType.GenericTypeArguments[0];
                        var array = Array.CreateInstance(elementType, values.Count);
                        for (var i = 0; i < values.Count; i++)
                        {
                            var convertedValue = ConvertQueryValue(elementType, values[i]);
                            array.SetValue(convertedValue, i);
                        }
                        property.SetValue(obj, array);
                    }
                    else
                    {
                        var firstValue = values.First();
                        var convertedValue = ConvertQueryValue(property.PropertyType, firstValue);
                        property.SetValue(obj, convertedValue);
                    }
                }
            }
            return obj;
        }

        static object ConvertQueryValue(Type type, string value)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsEnum)
            {
                return Enum.Parse(type, value);
            }
            else
            {
                return Convert.ChangeType(value, type);
            }
        }

        public static async Task<T> ParseBody<T>(this HttpContext context)
            where T : new()
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
        }

        public static async Task RespondJson(
            this HttpContext context,
            object data,
            int statusCode = 200)
        {
            var json = JsonConvert.SerializeObject(data, _serializerSettings);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(json);
            await context.Response.WriteAsync(json);
        }
    }
}
