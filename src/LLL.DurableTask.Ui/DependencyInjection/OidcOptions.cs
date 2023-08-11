using System.Text.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public class OidcOptions
{
    [JsonPropertyName("authority")]
    public string Authority { get; set; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("response_type")]
    public string ResponseType { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }

    [JsonPropertyName("display")]
    public string Display { get; set; }

    [JsonPropertyName("loadUserInfo")]
    public bool? LoadUserInfo { get; set; }

    [JsonPropertyName("automaticSilentRenew")]
    public bool AutomaticSilentRenew { get; set; }
}
