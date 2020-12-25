using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder
{
    public static class DurableTaskUiApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDurableTaskUI(this IApplicationBuilder builder, PathString pathMatch)
        {
            return builder.Map(pathMatch, pathBuilder =>
            {
                pathBuilder.UseDurableTaskUI();
            });
        }

        public static IApplicationBuilder UseDurableTaskUI(this IApplicationBuilder builder)
        {
            var fileProvider = new ManifestEmbeddedFileProvider(typeof(DurableTaskUiApplicationBuilderExtensions).Assembly, "/app/build");

            return builder
                .UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = fileProvider
                })
                .UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = fileProvider,
                    OnPrepareResponse = ctx =>
                    {
                        // CRA recommendations: https://create-react-app.dev/docs/production-build/#static-file-caching
                        if (ctx.Context.Request.Path.StartsWithSegments("/static"))
                            ctx.Context.Response.Headers[HeaderNames.CacheControl] = "max-age=31536000";
                        else
                            ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                    }
                });
        }
    }
}
