using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TwitchOverlapApi.Models;
using TwitchOverlapApi.Services;

namespace TwitchOverlapApi
{
    public class Startup
    {
        private const string AllowedCorsOrigins = "_allowedCorsOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(AllowedCorsOrigins, builder =>
                {
                    builder.WithOrigins("https://stats.roki.sh");
                });
            });

            services.AddHttpClient();
            services.Configure<TwitchDatabaseSettings>(Configuration.GetSection(nameof(TwitchDatabaseSettings)));
            services.AddSingleton<ITwitchDatabaseSettings>(sp => sp.GetRequiredService<IOptions<TwitchDatabaseSettings>>().Value);
            services.AddSingleton<TwitchService>();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddControllers();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseHttpsRedirection();

            app.UseRouting();
            
            app.UseCors(AllowedCorsOrigins);

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}