using Akka.Actor;
using Akka.Streams;
using DataViewer.Actors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace DataViewerClient
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
            services.AddControllers();
            services.AddHttpClient();

            services.AddSingleton(_ => ActorSystem.Create("dataviewer"));
            services.AddSingleton(provider =>
            {
                var actorSystem = provider.GetRequiredService<ActorSystem>();
                var materializer = actorSystem.Materializer(namePrefix: "httpMaterializer");
                var clientFactory = provider.GetService<IHttpClientFactory>();
                var actor = actorSystem
                    .ActorOf(
                        HttpDownloader.GetProp(materializer, clientFactory),
                        "httpDownloaderActor");

                return actor;
            });
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime life)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            life.ApplicationStarted.Register(
                () => app.ApplicationServices.GetService<ActorSystem>());
            life.ApplicationStopping.Register(
                () => app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait());
        }
    }
}
