using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointConventionBuilderExtensions
    {
        public static IEndpointConventionBuilder DisableAuthorization(this IEndpointConventionBuilder builder)
        {
            builder.Add(builder =>
            {
                var authorizeAttribute = builder.Metadata
                    .OfType<AuthorizeAttribute>()
                    .FirstOrDefault();

                if (authorizeAttribute != null)
                    builder.Metadata.Remove(authorizeAttribute);
            });

            return builder;
        }
    }
}