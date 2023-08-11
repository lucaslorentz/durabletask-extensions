using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LLL.DurableTask.Ui.Extensions;

static class HttpContextExtensions
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task RespondJson(
        this HttpContext context,
        object data,
        int statusCode = 200)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, _serializerOptions);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.Body.WriteAsync(jsonBytes);
    }
}
