using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddGrpc();

            // var mysqlConnectionString = "server=localhost;database=durabletask;user=root;password=root";

            services.AddDurableTaskEFCoreStorage()
                .UseInMemoryDatabase("Sample");
            // .UseNpgsql("Server=localhost;Port=5432;Database=durabletask;User Id=postgres;Password=root");
            // .UseMySql(mysqlConnectionString, ServerVersion.AutoDetect(mysqlConnectionString));
            // .UseSqlServer("server=localhost;database=durabletask;user=sa;password=P1ssw0rd");

            services.AddDurableTaskServer(builder =>
            {
                builder.AddGrpcEndpoints();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDurableTaskServerGrpcService();
            });
        }
    }
}
