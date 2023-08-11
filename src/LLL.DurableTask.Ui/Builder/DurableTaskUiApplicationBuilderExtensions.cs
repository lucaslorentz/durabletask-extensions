using LLL.DurableTask.Ui.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

public static class DurableTaskUiApplicationBuilderExtensions
{
    public static IApplicationBuilder UseDurableTaskUi(this IApplicationBuilder builder, PathString pathMatch)
    {
        return builder.Map(pathMatch, pathBuilder =>
        {
            pathBuilder.UseDurableTaskUi();
        });
    }

    public static IApplicationBuilder UseDurableTaskUi(this IApplicationBuilder builder)
    {
        var fileProvider = new ManifestEmbeddedFileProvider(typeof(DurableTaskUiApplicationBuilderExtensions).Assembly, "/app/build");

        return builder
            .Use(next => async context =>
            {
                if (!context.Request.Path.StartsWithSegments("/configuration.json"))
                {
                    await next(context);
                    return;
                }

                var options = context.RequestServices.GetRequiredService<IOptions<DurableTaskUiOptions>>()
                    .Value;

                await context.RespondJson(options);
            })
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
