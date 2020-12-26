namespace Microsoft.Extensions.DependencyInjection
{
    public class DurableTaskUiOptions
    {
        public string ApiBaseUrl { get; set; } = "/api";
        public OidcOptions Oidc { get; set; }
        public string[] UserNameClaims { get; set; }
    }
}