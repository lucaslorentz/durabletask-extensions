﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LLL.DurableTask.Api.Extensions;

static class HttpContextExtensions
{
    private static readonly JsonSerializerSettings _serializerSettings = new()
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
                if (property.PropertyType.IsGenericType && (
                    property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    || property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)))
                {
                    var elementType = property.PropertyType.GenericTypeArguments[0];
                    var array = Array.CreateInstance(elementType, values.Count);
                    for (var i = 0; i < values.Count; i++)
                    {
                        var convertedValue = ConvertQueryValue(elementType, values[i]);
                        array.SetValue(convertedValue, i);
                    }
                    SetProperty(property, obj, array);
                }
                else
                {
                    var firstValue = values.First();
                    var convertedValue = ConvertQueryValue(property.PropertyType, firstValue);
                    SetProperty(property, obj, convertedValue);
                }
            }
        }
        return obj;
    }

    static void SetProperty(PropertyInfo property, object target, object value)
    {
        if (property.CanWrite)
        {
            property.SetValue(target, value);
            return;
        }

        var propertyValue = property.GetValue(target);
        if (propertyValue is IDictionary targetDict && value is IDictionary dictValue)
        {
            foreach (var key in dictValue.Keys)
                targetDict.Add(key, dictValue[key]);
            return;
        }

        throw new Exception($"Property {property.Name} cannot be written");
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
            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Object:
                    return JsonConvert.DeserializeObject(value, type);
                default:
                    return Convert.ChangeType(value, type);
            }

        }
    }

    public static async Task<T> ParseBody<T>(this HttpContext context)
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
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(json);
    }
}
