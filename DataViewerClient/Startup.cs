using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using DataViewer;
using DataViewer.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                var clientFactory = provider.GetService<IHttpClientFactory>();
                var actor = provider
                    .GetRequiredService<ActorSystem>()
                    .ActorOf(
                        HttpDownloader.GetProp(clientFactory),
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

    public static class ActorProviders
    {
        public delegate IActorRef HttpDownloaderProvider();
    }
}
