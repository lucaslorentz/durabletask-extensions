using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder
{
    public static class DurableTaskUiApplicationBuilderExtensions
    {
        private static readonly HashSet<string> _noCacheExtensions = new[]
        {
            ".htm",
            ".html"
        }.ToHashSet(StringComparer.OrdinalIgnoreCase);

        private const int _cacheDurationInSeconds = 60 * 60 * 24 * 365;

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
                        if (!_noCacheExtensions.Contains(Path.GetExtension(ctx.File.Name)))
                        {
                            ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + _cacheDurationInSeconds;
                        }
                    }
                });
        }
    }
}
