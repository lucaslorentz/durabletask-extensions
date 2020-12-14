using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    };
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            services.AddGrpc();

            services.AddDurableTaskEFCoreStorage()
                //.UseNpgsql("Server=localhost;Port=5432;Database=durabletask;User Id=postgres;Password=root");
                .UseMySql("server=localhost;database=durabletask;user=root;password=root");
                //.UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd");

            services.AddDurableTaskServer(builder =>
            {
                builder.AddGrpcEndpoints();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapTaskHubServerGrpcEndpoints();
            });
        }
    }
}
